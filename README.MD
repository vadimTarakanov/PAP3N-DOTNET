This library allows you to connect to Winctrl 737 MCP programmatically to:
1) Read status of button presses by subscribing to change events
2) Set each LED on or off
3) Display text on the 7 segment displays with support for decimal points and special features like +/- symbols on vertical speedi indicator, etc
4) Control the A/T Solenoid same way as an LED (ie case 2)

This has not been battle tested and Im not 100% sure if it will work with all versions of PAP3N out there as I only can test on mine, but you can adapt and change the code as needed, or send the device to me along with a case of beer ;)

How was this done?
1) Install Wireshark
2) Observe what reports are sent between your machine and the device, as you play around with settings Winctrl's SimAppPro. For example, I found that:

    a) Displays are controlled as a single 128 byte report/package sent to the device. Inside it are bit encoded (that is each segment corresponds to some bit) displays and decimal points
    
    b) Button presses are sent as a single report, where each button is sent as a bit somewhere in the report (see the code for reference)

    c) Position of rotrary encoders is sent as a 16 bit counters and individual button presses; I only tap into the counters.


Provided example code connects to the device and demonstrates most of the functionality of how to process input and send output.