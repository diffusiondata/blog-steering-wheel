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

#import "F1PedalView.h"

@implementation F1PedalView
{
    CALayer* _barLayer;
}

@synthesize value = _value;

- (id)initWithFrame:(const CGRect)frame
{
    if (!(self = [super initWithFrame:frame])) return nil;
    [self createSubLayers];
    return self;
}

-(id)initWithCoder:(NSCoder *const)coder
{
    if (!(self = [super initWithCoder:coder])) return nil;
    [self createSubLayers];
    return self;
}

-(void)createSubLayers
{
    NSLog(@"createSubLayers");
    
    _barLayer = [CALayer layer];
    _barLayer.backgroundColor = [UIColor greenColor].CGColor;
    [self.layer addSublayer:_barLayer];
}

-(void)layoutSublayersOfLayer:(CALayer *const)layer
{
    [self scaleLayers];
}

-(void)setValue:(const double)value
{
    _value = value;
    [self scaleLayers];
}

-(void)scaleLayers
{
    const CGFloat h = (CGFloat)(self.layer.bounds.size.height * _value);
    const CGFloat offset = (CGFloat)(self.layer.bounds.size.height - h) / 2.0f;
    
    [CATransaction begin];
    [CATransaction setAnimationDuration:0.0]; // disable implicit basic animation duration (we don't want delay)
    _barLayer.frame = CGRectMake(self.layer.bounds.origin.x, self.layer.bounds.origin.y + offset, self.layer.bounds.size.width, h);
    [CATransaction commit];
}

@end
