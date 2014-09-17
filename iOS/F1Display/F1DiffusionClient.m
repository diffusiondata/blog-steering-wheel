/*
 * Copyright (C) 2014 Push Technology Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

#import "F1DiffusionClient.h"
#import "diffusion.h"

// Internally redefining Key-Value Observable Properties as readwrite to get KVO for 'free' on auto synthesize.

@interface F1DiffusionClient ()
@property(nonatomic, readwrite) F1DiffusionClientState state;
@property(nonatomic, readwrite) double steering;
@property(nonatomic, readwrite) double braking;
@property(nonatomic, readwrite) double acceleration;
@property(nonatomic, readwrite) NSUInteger gear;
@property(nonatomic, readwrite) NSArray* buttonStates;
@property(nonatomic, readwrite) NSArray* buttonNames;
@property(nonatomic, readwrite) F1RefreshInterval refreshInterval;
-(void)updateButtonStatesFromMessage:(DFTopicMessage *)message;
-(void)updateButtonNamesFromMessage:(DFTopicMessage *)message;
-(void)updateRefreshIntervalFromMessage:(DFTopicMessage *)message;
@end

@interface F1DiffusionMetrics ()
@property(nonatomic, readwrite) UInt64 countOfUpdates;
@property(nonatomic, readwrite) UInt64 upTimeInSeconds;
@property(nonatomic, readwrite) UInt64 countOfSuccessfulTopicSourceUpdates;
@property(nonatomic, readwrite) UInt64 countOfFailedTopicSourceUpdates;
@property(nonatomic, readwrite) UInt64 rateOfUpdatesPerSecond;
@property(nonatomic, readwrite) UInt64 rateOfSuccessfulTopicSourceUpdatesPerSecond;
@end

@interface F1DiffusionClient (DFClientDelegate) <DFClientDelegate>
@end

static NSString *const F1DiffusionClientErrorDomain = @"F1DiffusionClientErrorDomain";

typedef NS_ENUM(NSInteger, F1DiffusionClientErrorCode)
{
    F1DiffusionClientErrorCode_Unknown = 1,
    F1DiffusionClientErrorCode_FailedToCreateConnectionDetails = 2,
    F1DiffusionClientErrorCode_FailedToCreateClient = 3,
};

typedef void (^MessageHandler)(DFTopicMessage * message);

static NSError* _createError(const F1DiffusionClientErrorCode code)
{
    return [NSError errorWithDomain:F1DiffusionClientErrorDomain code:code userInfo:nil];
}

static NSError* _enforceError(NSError *const error)
{
    return error ? error : _createError(F1DiffusionClientErrorCode_Unknown);
}

static void _addMessageHandler(NSMutableDictionary *const d, NSString *const topicPath, MessageHandler handler)
{
    [d setObject:handler forKey:topicPath];
}

static NSDictionary* _createMessageHandlers(F1DiffusionClient *const strongSelf)
{
    F1DiffusionClient __weak *const weakSelf = strongSelf;
    NSMutableDictionary *const d = [NSMutableDictionary new];
    
    _addMessageHandler(d, @"Metrics/CountOfUpdates", ^(DFTopicMessage *const message)
    {
        weakSelf.metrics.countOfUpdates = (UInt64)[[message asString] longLongValue];
    });
    
    _addMessageHandler(d, @"Metrics/RateOfUpdatesPerSecond", ^(DFTopicMessage *const message)
    {
        weakSelf.metrics.rateOfUpdatesPerSecond = (UInt64)[[message asString] longLongValue];
    });
    
    _addMessageHandler(d, @"Metrics/RateOfSuccessfulTopicSourceUpdatesPerSecond", ^(DFTopicMessage *const message)
    {
        weakSelf.metrics.rateOfSuccessfulTopicSourceUpdatesPerSecond = (UInt64)[[message asString] longLongValue];
    });

    _addMessageHandler(d, @"Steering", ^(DFTopicMessage *const message)
    {
        weakSelf.steering = [[message asString] doubleValue];
    });

    _addMessageHandler(d, @"Braking", ^(DFTopicMessage *const message)
    {
        weakSelf.braking = [[message asString] doubleValue];
    });
    
    _addMessageHandler(d, @"Acceleration", ^(DFTopicMessage *const message)
    {
        weakSelf.acceleration = [[message asString] doubleValue];
    });
    
    _addMessageHandler(d, @"Gear", ^(DFTopicMessage *const message)
    {
        weakSelf.gear = (NSUInteger)[[message asString] integerValue];
    });

    _addMessageHandler(d, @"Buttons/States", ^(DFTopicMessage *const message)
    {
        [weakSelf updateButtonStatesFromMessage:message];
    });
    
    _addMessageHandler(d, @"Buttons/Names", ^(DFTopicMessage *const message)
    {
        [weakSelf updateButtonNamesFromMessage:message];
    });

    _addMessageHandler(d, @"RefreshInterval", ^(DFTopicMessage *const message)
    {
        [weakSelf updateRefreshIntervalFromMessage:message];
    });
    
    return [d copy];
}

@implementation F1DiffusionClient
{
    NSString* _rootTopicPath;
    DFClient* _client;
    NSDictionary* _messageHandlers; // Key is topic path (NSString) and Value is handler block (MessageHandler)
}

@synthesize state = _state;
@synthesize serverURL = _serverURL;

@synthesize metrics = _metrics;

@synthesize steering = _steering;
@synthesize braking = _braking;
@synthesize acceleration = _acceleration;
@synthesize gear = _gear;
@synthesize buttonStates = _buttonStates;
@synthesize buttonNames = _buttonNames;
@synthesize refreshInterval = _refreshInterval;

-(id)initWithServerURL:(NSURL *const)serverURL
         rootTopicPath:(NSString *const)rootTopicPath
{
    if (!serverURL) [NSException raise:NSInvalidArgumentException format:@"serverURL is nil."];
    if (!rootTopicPath) [NSException raise:NSInvalidArgumentException format:@"rootTopicPath is nil."];
    
    if (!(self = [super init])) return nil;
    
    _serverURL = serverURL;
    _rootTopicPath = [rootTopicPath copy];
    _metrics = [F1DiffusionMetrics new];
    _messageHandlers = _createMessageHandlers(self);
    
    return self;
}

-(NSError *)connect
{
    if (F1DiffusionClientState_NotConnected != self.state)
        return nil; // Nothing to do
    
    NSError* error;
    
    DFServerDetails *const serverDetails = [[DFServerDetails alloc] initWithURL:_serverURL error:&error];
    if (!serverDetails)
        return _enforceError(error); // Failed
    static NSNumber * timeoutInSeconds = nil; // static due to timeout property on serverDetails being weak
    if (!timeoutInSeconds)
        timeoutInSeconds = [NSNumber numberWithInteger:5];
    serverDetails.timeout = timeoutInSeconds;
    
    DFConnectionDetails *const connectionDetails = [[DFConnectionDetails alloc] initWithServer:serverDetails topics:nil andCredentials:nil];
    if (!connectionDetails)
        return _createError(F1DiffusionClientErrorCode_FailedToCreateConnectionDetails); // Failed
    
    DFClient *const client = [DFClient new];
    if (!client)
        return _createError(F1DiffusionClientErrorCode_FailedToCreateClient);
    
    client.connectionDetails = connectionDetails;
    client.delegate = self;
    
    self.state = F1DiffusionClientState_Connecting;
    [client connect];
    
    // Success
    _client = client;
    return nil;
}

typedef id (^Transformer)(NSString * field);

-(NSArray *)updateArray:(NSArray *const)array fromMessage:(DFTopicMessage *const)message withTransformer:(const Transformer)transformer
{
    NSArray *const fields = [message getFields:0];
    const NSUInteger countOfFields = fields.count;
    if (countOfFields < 1)
        return nil;
    
    NSMutableArray *const newArray = (array && countOfFields==array.count) ? [array mutableCopy] : [NSMutableArray arrayWithCapacity:countOfFields];
    NSUInteger index = 0;
    for (NSString *const field in fields)
    {
        // if this field is empty (zero length) then it's not being set (probably a delta).
        if (0 != field.length)
            [newArray setObject:(transformer ? transformer(field) : field) atIndexedSubscript:index];
        index++;
    }

    return [newArray copy];
}

-(void)updateButtonStatesFromMessage:(DFTopicMessage *const)message
{
    self.buttonStates = [self updateArray:self.buttonStates
                              fromMessage:message
                          withTransformer:^id(NSString *const field)
    {
        return [NSNumber numberWithBool:[field isEqualToString:@"1"]];
    }];
}

-(void)updateButtonNamesFromMessage:(DFTopicMessage *const)message
{
    self.buttonNames = [self updateArray:self.buttonNames
                            fromMessage:message
                        withTransformer:nil];
}

-(void)updateRefreshIntervalFromMessage:(DFTopicMessage *const)message
{
    // ensure that we've received exactly two fields
    NSArray *const fields = [message getFields:0];
    if (2 != fields.count) return;
    NSString *const frequency = fields[0];
    NSString *const sleepDuration = fields[1];
    
    // cater for initial load as well as delta (where field may be 'empty')
    F1RefreshInterval refreshInterval = self.refreshInterval;
    if (frequency.length > 0)
        refreshInterval.frequency = (NSUInteger)[frequency integerValue];
    if (sleepDuration.length > 0)
        refreshInterval.sleepDuration = (NSUInteger)[sleepDuration integerValue];
    
    // use 'internal' setter to assign back to instance variable and trigger KVO notifications
    self.refreshInterval = refreshInterval;
}

-(void)disconnect
{
    if (F1DiffusionClientState_NotConnected == self.state)
        return; // Nothing to do
    
    self.state = F1DiffusionClientState_Disconnecting;
    [_client close];
}

-(void)teardown
{
    self.state = F1DiffusionClientState_NotConnected;
    _client.delegate = nil; // just in case
    _client = nil; // as I've found I can't 'reuse' it
}

@end

@implementation F1DiffusionMetrics

@synthesize countOfUpdates = _countOfUpdates;
@synthesize upTimeInSeconds = _upTimeInSeconds;
@synthesize countOfSuccessfulTopicSourceUpdates = _countOfSuccessfulTopicSourceUpdates;
@synthesize countOfFailedTopicSourceUpdates = _countOfFailedTopicSourceUpdates;
@synthesize rateOfUpdatesPerSecond = _rateOfUpdatesPerSecond;
@synthesize rateOfSuccessfulTopicSourceUpdatesPerSecond = _rateOfSuccessfulTopicSourceUpdatesPerSecond;

@end

@implementation F1DiffusionClient (DFClientDelegate)

-(void)onConnection:(BOOL)isConnected
{
    NSLog(@"onConnection: %@", (isConnected ? @"YES" : @"NO"));
    if (isConnected)
    {
        self.state = F1DiffusionClientState_Connected;
        
        // I had to look at the client SDK source code to discover that this cannot be called until we're connected!
        [_client subscribe:_rootTopicPath];
    }
    else
    {
        // this has been observed when we've failed to connect due to a timeout
        [self teardown];
    }
}

-(void)onLostConnection
{
    NSLog(@"onLostConnection");
    [self teardown];
}

-(void)onAbort
{
    NSLog(@"onAbort");
}

-(void)onMessage:(DFTopicMessage *const)message
{
    NSString *const subTopicPath = [message.topic substringFromIndex:_rootTopicPath.length];
    const MessageHandler handler = [_messageHandlers objectForKey:subTopicPath];
    if (!handler) return;
    handler(message);
}

-(void)onPing:(DFPingMessage *const)message
{
    NSLog(@"onPing: %@", message);
}

-(void)onServerRejectedConnection
{
    NSLog(@"onServerRejectedConnection");
}

-(void)onMessageNotAcknowledged:(DFTopicMessage *const)message
{
    NSLog(@"onMessageNotAcknowledged: %@", message);
}

-(void)onConnectionSequenceExhausted:(DFClient *const)client
{
    NSLog(@"onConnectionSequenceExhausted: %@", client);
}

@end