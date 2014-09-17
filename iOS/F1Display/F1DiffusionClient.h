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

#import <Foundation/Foundation.h>

typedef struct
{
    NSUInteger frequency;
    NSUInteger sleepDuration;
} F1RefreshInterval;

@class F1DiffusionMetrics;

typedef NS_ENUM(NSInteger, F1DiffusionClientState)
{
    F1DiffusionClientState_NotConnected = 0,
    F1DiffusionClientState_Connecting,
    F1DiffusionClientState_Connected,
    F1DiffusionClientState_Disconnecting,
};

@interface F1DiffusionClient : NSObject

/*
 Neither serverURL or rootTopicPath may be nil.
 */
-(id)initWithServerURL:(NSURL *)serverURL
         rootTopicPath:(NSString *)rootTopicPath;

@property(atomic, readonly) F1DiffusionClientState state; // Key-Value Observable
@property(nonatomic, readonly) NSURL* serverURL;

-(NSError *)connect; // returns nil (Success) if now connecting or was already connected
-(void)disconnect;

@property(nonatomic, readonly) F1DiffusionMetrics* metrics;

// Key-Value Observable Properties
@property(nonatomic, readonly) double steering;
@property(nonatomic, readonly) double braking;
@property(nonatomic, readonly) double acceleration;
@property(nonatomic, readonly) NSUInteger gear;
@property(nonatomic, readonly) NSArray* buttonStates; // of NSNumber (boxing BOOL)
@property(nonatomic, readonly) NSArray* buttonNames; // of NSString
@property(nonatomic, readonly) F1RefreshInterval refreshInterval;

@end

@interface F1DiffusionMetrics : NSObject

// Key-Value Observable Properties
@property(nonatomic, readonly) UInt64 countOfUpdates;
@property(nonatomic, readonly) UInt64 upTimeInSeconds;
@property(nonatomic, readonly) UInt64 countOfSuccessfulTopicSourceUpdates;
@property(nonatomic, readonly) UInt64 countOfFailedTopicSourceUpdates;
@property(nonatomic, readonly) UInt64 rateOfUpdatesPerSecond;
@property(nonatomic, readonly) UInt64 rateOfSuccessfulTopicSourceUpdatesPerSecond;

@end
