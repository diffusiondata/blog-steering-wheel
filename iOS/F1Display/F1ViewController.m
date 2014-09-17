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

#import "F1ViewController.h"
#import "F1DiffusionClient.h"
#import "MAKVONotificationCenter.h"
#import "F1SteeringWheelView.h"
#import "F1PedalView.h"
#import "CONFIG.h"

@interface F1ViewController (UIAlertViewDelegate) <UIAlertViewDelegate>
@end

@implementation F1ViewController
{
    F1DiffusionClient* _diffusionClient;
    
    IBOutlet __weak UILabel* _rateOfUpdatesPerSecondLabel;
    IBOutlet __weak UILabel* _rateOfSuccessfulTopicSourceUpdatesPerSecondLabel;
    IBOutlet __weak UILabel* _countOfUpdatesLabel;
    IBOutlet __weak F1SteeringWheelView* _steeringWheelView;
    IBOutlet __weak UIView* _connectionStateView;
    IBOutlet __weak F1PedalView* _brakePedalView;
    IBOutlet __weak F1PedalView* _accelerationPedalView;
    IBOutlet __weak UILabel* _gearLabel;
    IBOutlet __weak UILabel* _buttonStatesLabel;
    IBOutlet __weak UILabel* _buttonNamesLabel;
    IBOutlet __weak UILabel* _refreshRateLabel;
    
    id _applicationDidBecomeActiveNotificationObserver;
    id _applicationDidEnterBackgroundNotificationObserver;
    
    BOOL _disconnectedByMe;
    UIAlertView* _noConnectionAlertView;
}

-(void)viewDidLoad
{
    [super viewDidLoad];
    
    [self resetViews];

    NSURL* const diffusionServerURL = [NSURL URLWithString:DIFFUSION_SERVER_URL];
    _diffusionClient = [[F1DiffusionClient alloc] initWithServerURL:diffusionServerURL rootTopicPath:@"F1Publisher/"];
    
    [[self class] hookUpdatesForDiffusionClient:_diffusionClient withViewController:self];
}

+(void)hookApplicationNotificationsWithViewController:(F1ViewController *const)vc
{
    F1ViewController __weak *const weakSelf = vc;
    
    if (!vc->_applicationDidBecomeActiveNotificationObserver)
    {
        vc->_applicationDidBecomeActiveNotificationObserver = [[NSNotificationCenter defaultCenter] addObserverForName:UIApplicationDidBecomeActiveNotification
                                                                                                                object:nil
                                                                                                                 queue:nil
                                                                                                            usingBlock:^(NSNotification *const note)
        {
            [weakSelf connectDiffusion];
        }];
    }
    
    if (!vc->_applicationDidEnterBackgroundNotificationObserver)
    {
        vc->_applicationDidEnterBackgroundNotificationObserver = [[NSNotificationCenter defaultCenter] addObserverForName:UIApplicationDidEnterBackgroundNotification
                                                                                                                   object:nil
                                                                                                                    queue:nil
                                                                                                               usingBlock:^(NSNotification *const note)
        {
            [weakSelf disconnectDiffusion];
        }];
    }
}

-(void)unhookApplicationNotifications
{
    if (_applicationDidBecomeActiveNotificationObserver)
        [[NSNotificationCenter defaultCenter] removeObserver:_applicationDidBecomeActiveNotificationObserver];
    _applicationDidBecomeActiveNotificationObserver = nil;
    
    if (_applicationDidEnterBackgroundNotificationObserver)
        [[NSNotificationCenter defaultCenter] removeObserver:_applicationDidEnterBackgroundNotificationObserver];
    _applicationDidEnterBackgroundNotificationObserver = nil;
}

-(void)connectDiffusion
{
    NSLog(@"connectDiffusion");
    
    if (_noConnectionAlertView)
        [_noConnectionAlertView dismissWithClickedButtonIndex:0 animated:NO]; // does NOT trigger alertView:clickedButtonAtIndex:
    _noConnectionAlertView = nil;

    _disconnectedByMe = NO;
    [_diffusionClient connect];
}

-(void)disconnectDiffusion
{
    NSLog(@"disconnectDiffusion");
    
    _disconnectedByMe = YES;
    [_diffusionClient disconnect];
}

-(void)viewDidAppear:(const BOOL)animated
{
    [super viewDidAppear:animated];
    [self connectDiffusion];
    [[self class] hookApplicationNotificationsWithViewController:self];
}

-(void)viewWillDisappear:(const BOOL)animated
{
    [super viewDidDisappear:animated];
    [self unhookApplicationNotifications];
    [self disconnectDiffusion];
}

-(void)showNoConnectionAlertView
{
    if (_noConnectionAlertView)
        return; // Already visible
    
    NSString *const message = [NSString stringWithFormat:@"Failed to connect to Diffusion server at \"%@\".", _diffusionClient.serverURL];
    _noConnectionAlertView = [[UIAlertView alloc] initWithTitle:@"No Connection"
                                                        message:message
                                                       delegate:self
                                              cancelButtonTitle:nil
                                              otherButtonTitles:@"Retry", nil];
    [_noConnectionAlertView show];
}

-(void)resetViews
{
    _rateOfUpdatesPerSecondLabel.text = nil;
    _rateOfSuccessfulTopicSourceUpdatesPerSecondLabel.text = nil;
    _countOfUpdatesLabel.text = nil;
    _steeringWheelView.value = 0.0;
    _brakePedalView.value = 0.0;
    _accelerationPedalView.value = 0.0;
    _gearLabel.text = nil;
    _buttonStatesLabel.text = nil;
    _buttonNamesLabel.text = nil;
    _refreshRateLabel.text = nil;
}

static NSString *const _RefreshIntervalFormat = @"/ %lu (%lums)";

-(void)syncViews
{
    _rateOfUpdatesPerSecondLabel.text = [NSString stringWithFormat:@"%llu", _diffusionClient.metrics.rateOfUpdatesPerSecond];
    _rateOfSuccessfulTopicSourceUpdatesPerSecondLabel.text = [NSString stringWithFormat:@"%llu", _diffusionClient.metrics.rateOfSuccessfulTopicSourceUpdatesPerSecond];
    _countOfUpdatesLabel.text = [NSString stringWithFormat:@"%llu", _diffusionClient.metrics.countOfUpdates];
    _steeringWheelView.value = _diffusionClient.steering;
    _brakePedalView.value = _diffusionClient.braking;
    _accelerationPedalView.value = _diffusionClient.acceleration;
    _gearLabel.text = [NSString stringWithFormat:@"%lu", (unsigned long)_diffusionClient.gear];
    _buttonStatesLabel.text = [[self class] formatButtonStates:_diffusionClient.buttonStates];
    F1RefreshInterval refreshInterval = _diffusionClient.refreshInterval;
    _refreshRateLabel.text = [NSString stringWithFormat:_RefreshIntervalFormat, (unsigned long)refreshInterval.frequency, (unsigned long)refreshInterval.sleepDuration];
    
    [self updateActiveButtonNamesDisplay];
}

-(void)updateActiveButtonNamesDisplay
{
    NSArray *const buttonStates = _diffusionClient.buttonStates;
    NSArray *const buttonNames = _diffusionClient.buttonNames;
    
    NSMutableString *const display = [NSMutableString new];
    for (NSUInteger i=0; i<MIN(buttonNames.count, buttonStates.count); i++)
    {
        if ([(NSNumber*)buttonStates[i] boolValue])
        {
            NSString *const buttonName = buttonNames[i];
            if (buttonName)
            {
                if (display.length > 0)
                    [display appendString:@", "];
                [display appendString:buttonName];
            }
        }
    }
    
    _buttonNamesLabel.text = display;
}

+(NSString *)formatButtonStates:(NSArray *const)buttonStates
{
    NSMutableString *const formattedButtons = [NSMutableString stringWithCapacity:(buttonStates.count * 2) - 1];
    for (NSNumber *const buttonState in buttonStates)
    {
        if (formattedButtons.length > 0)
            [formattedButtons appendString:@" "];
        [formattedButtons appendString:[buttonState boolValue] ? @"1" : @"0"];
    }
    return formattedButtons;
}

typedef void (^DiffusionHandler)(F1ViewController* vc, id value);

+(void)hookSource:(const id)source
          keyPath:(NSString *const)keyPath
   viewController:(F1ViewController *const)vc
          handler:(const DiffusionHandler)handler
{
    F1ViewController __weak *const weakSelf = vc;
    [source addObserver:vc
                keyPath:keyPath
                options:NSKeyValueObservingOptionNew
                  block:^(MAKVONotification *const notification)
    {
        F1ViewController *const strongSelf = weakSelf;
        if (!strongSelf) return;
        handler(strongSelf, notification.newValue);
    }];
}

+(void)hookUpdatesForDiffusionClient:(F1DiffusionClient *const)diffusionClient
                  withViewController:(F1ViewController *const)vc
{
    // STATE
    
    [self hookSource:diffusionClient
             keyPath:@"state"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        NSNumber *const number = (NSNumber *)value;
        const F1DiffusionClientState state = (F1DiffusionClientState)number.integerValue;
        switch (state)
        {
            case F1DiffusionClientState_NotConnected:
                vc->_connectionStateView.backgroundColor = [UIColor redColor];
                if (!vc->_disconnectedByMe)
                    [vc showNoConnectionAlertView];
                vc->_disconnectedByMe = NO;
                [vc resetViews];
                break;
                
            case F1DiffusionClientState_Connected:
                vc->_connectionStateView.backgroundColor = [UIColor greenColor];
                [vc syncViews];
                break;
                
            case F1DiffusionClientState_Connecting:
            case F1DiffusionClientState_Disconnecting:
                vc->_connectionStateView.backgroundColor = [UIColor yellowColor];
                [vc resetViews];
                break;
        }
    }];
    
    // STEERING

    [self hookSource:diffusionClient
             keyPath:@"steering"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_steeringWheelView.value = [(NSNumber*)value doubleValue];
    }];
    
    // BRAKING
    
    [self hookSource:diffusionClient
             keyPath:@"braking"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_brakePedalView.value = [(NSNumber*)value doubleValue];
    }];
    
    // ACCELERATION

    [self hookSource:diffusionClient
             keyPath:@"acceleration"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_accelerationPedalView.value = [(NSNumber*)value doubleValue];
    }];
    
    // GEAR
    
    [self hookSource:diffusionClient
             keyPath:@"gear"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_gearLabel.text = [value description];
    }];
    
    // BUTTON STATES
    
    [self hookSource:diffusionClient
             keyPath:@"buttonStates"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_buttonStatesLabel.text = [self formatButtonStates:(NSArray*)value];
        [vc updateActiveButtonNamesDisplay];
    }];
    
    // BUTTON NAMES
    
    [self hookSource:diffusionClient
             keyPath:@"buttonNames"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        [vc updateActiveButtonNamesDisplay];
    }];
    
    // REFRESH RATE
    
    [self hookSource:diffusionClient
             keyPath:@"refreshInterval"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        // check that the KVO-boxed struct is of the correct type (getValue: could crash our process)
        NSValue *const v = value;
        const char *const objCType = [v objCType];
        if (!objCType) return; // just in case
        if (0 != strcmp(objCType, @encode(F1RefreshInterval))) return; // wrong type

        // we're now confident that v is boxing an F1RefreshInterval struct
        F1RefreshInterval refreshInterval;
        [(NSValue*)value getValue:&refreshInterval];
        vc->_refreshRateLabel.text = [NSString stringWithFormat:_RefreshIntervalFormat, (unsigned long)refreshInterval.frequency, (unsigned long)refreshInterval.sleepDuration];
    }];
    
    // METRICS
    
    // Rate Of Updates Per Second
    [self hookSource:diffusionClient.metrics
             keyPath:@"rateOfUpdatesPerSecond"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_rateOfUpdatesPerSecondLabel.text = [value description];
    }];
    
    // Rate Of Successful Topic Source Updates Per Second
    [self hookSource:diffusionClient.metrics
             keyPath:@"rateOfSuccessfulTopicSourceUpdatesPerSecond"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_rateOfSuccessfulTopicSourceUpdatesPerSecondLabel.text = [value description];
    }];
    
    // Count Of Updates
    [self hookSource:diffusionClient.metrics
             keyPath:@"countOfUpdates"
      viewController:vc
             handler:^(F1ViewController *const vc, const id value)
    {
        vc->_countOfUpdatesLabel.text = [value description];
    }];
}

@end

@implementation F1ViewController (UIAlertViewDelegate)

-(void)alertView:(UIAlertView *const)alertView clickedButtonAtIndex:(const NSInteger)buttonIndex
{
    NSLog(@"Clicked Alert View Button: %ld", (long)buttonIndex);
    _noConnectionAlertView = nil;
    [self connectDiffusion];
}

@end