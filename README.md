# Introduction

This project aims to demonstrate the realtime data transmission capabilities of Diffusion&trade;.

It does this by using a steering wheel device, as used for gaming, as a source of constantly changing
data. As the driver interacts with the wheel, their actions are reflected in realtime by the display
clients.

# Publisher

Connects to the Diffusion server using the *Unified API*. It streams data about the car and the
driver's use of the steer wheel to topics on the server. To do this it uses the following features:

- **TopicControl**: To create the topics to which we're going to publish.
- **Topics**: To check whether the topics have already been created. It does this using the
  simplistic approach of calling `GetTopicDetails` just for our root topic "F1Publisher".
- **TopicUpdateControl**: To publish updates to the topics.

Implemented in C# (.NET) for Microsoft Windows:

- Developed and tested using
  [Visual Studio Express](http://www.visualstudio.com/en-us/products/visual-studio-express-vs.aspx)
  2013 for Windows Desktop under Windows 7.
- Requires the Diffusion .NET Unified Client Library (SDK) version 5.1.0 or higher.
- Requires the [SlimDX framework](http://www.slimdx.org) (tested with the January 2012 release).
- Requires a Joystick device (ideally Steering Wheel) supporting DirectInput (DirectX).
- Runs as a console application.

Windows was chosen as the implementation platform for the publisher due to its high level of support
for gaming devices. DirectInput wrapped with SlimDX provides easy, immediate access to Joystick events with
minimal code.

Tested in development using a
[Thrustmaster Ferrari Challenge Wheel](http://www.thrustmaster.com/products/ferrari-challenge-racing-wheel-pc-ps3).
In order to attain correct button mappings you will probably need to install the manufacturer's own device driver
rather than relying on the default Windows driver.

# Display Client

Connects to the Diffusion server using the *Classic API*.

Implemented in Objective-C ([Cocoa Touch](https://developer.apple.com/technologies/ios/cocoa-touch.html))
for Apple iOS mobile devices (iPhone, iPad and iPod):

- Developed and tested using [Xcode](https://developer.apple.com/xcode/) 5.1.1 (iOS SDK 7.1).
- Requires the Diffusion iOS Library (SDK).
- iOS Deployment Target: 7.0.

# Running the Demo

## Prerequisites

You'll need:

- An OS X machine with Apple's Xcode installed.
- Optionally, an iOS device provisioned for development testing. Otherwise, the iOS simulator provided with Xcode.
- A Windows machine with Microsoft's Visual Studio installed.
- A DirectX-compatible steering wheel or joystick gaming device plugged in to the Windows machine.
- A machine running Diffusion 5.1.
- A network setup which allows both your Windows machine and your iOS device (physical or simulated via OS X) to communicate with the Diffusion server.

## 1. Start Diffusion

Get your Diffusion server up and running. The default configuration is fine as this enables support for clients using both the Classic and Unified APIs.

## 2. Start the Publisher

On your Windows machine:

1. Clone our demo repository.
2. Open the solution in Visual Studio: `Windows/F1.sln`
3. Unless you're running the Diffusion server on the same machine, you'll need to navigate to **Settings** (available under **Properties** for the `F1Publisher` project) and modify the value for `DiffusionServerURL`. By default this is set to `dpt://localhost:8081`
4. **Start** (Build and Run).

## 3. Start the Display Client(s)

On your OS X machine:

1. Clone our demo repository.
2. Open the project in Xcode: `iOS/F1Display.xcodeproj`
3. Configure the location of your Diffusion server. This is defined by the macro `DIFFUSION_SERVER_URL` in `CONFIG.h`
4. **Run**