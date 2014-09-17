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

#import "F1SteeringWheelView.h"

@implementation F1SteeringWheelView
{
    CALayer* _steeringWheelLayer;
    CGFloat _imageSize;
    CGFloat _scale;
    CGPoint _offset;
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
    UIImage *const image = [UIImage imageNamed:@"SteeringWheel"];
    
    _steeringWheelLayer = [CALayer layer];
    _steeringWheelLayer.contents = (__bridge id)(image.CGImage);
    _steeringWheelLayer.frame = CGRectMake(0, 0, image.size.width, image.size.height);
    _imageSize = MAX(image.size.width, image.size.height);
    [self.layer addSublayer:_steeringWheelLayer];
}

-(void)layoutSublayersOfLayer:(CALayer *const)layer
{
    const CGFloat size = MIN(layer.bounds.size.width, layer.bounds.size.height);
    _scale = size / _imageSize;
    const CGFloat offset = -((_imageSize - size) / 2.0f);
    const CGFloat xOffset = offset + ((layer.bounds.size.width - size) / 2.0f);
    const CGFloat yOffset = offset + ((layer.bounds.size.height - size) / 2.0f);
    _offset = CGPointMake(xOffset, yOffset);
    [self applyTransform];
}

-(void)setValue:(const double)value
{
    _value = value;
    [self applyTransform];
}

-(void)applyTransform
{
    CGAffineTransform rotateTransform = CGAffineTransformMakeRotation((CGFloat)(M_PI * _value));
    CGAffineTransform scaleTransform = CGAffineTransformMakeScale(_scale, _scale);
    CGAffineTransform scaleRotateTransform = CGAffineTransformConcat(rotateTransform, scaleTransform);
    
    CGAffineTransform translateTransform = CGAffineTransformMakeTranslation(_offset.x, _offset.y);
    CGAffineTransform transform = CGAffineTransformConcat(scaleRotateTransform, translateTransform);
 
    [CATransaction begin];
    [CATransaction setAnimationDuration:0.0]; // disable implicit basic animation duration (we don't want delay)
    [_steeringWheelLayer setAffineTransform:transform];
    [CATransaction commit];
}

@end
