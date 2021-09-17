# FlatPanelDisplay_Panasonic_THXXCQ1U_IP Crestron Certified Driver
You will need to add the following to ```/lib```:
```
* Crestron.DeviceDrivers.API.dll
* CrestronCertifiedDriverResourcesLibrary.dll
* RADCommon.dll
* RADDisplay.dll
```
## Questions/Comments/Tomatoes/Tears
### In file PanasonicTHXXCQ1UTcp.cs
* Is there an event post-load for receiving ```DefaultUserame``` and ```DefaultPassword```, or is passing it into the protocol's ```Initialize``` method a good way to handle it?
* I didn't have a hostname to test with. Does the Tcp2 code look correct?
* I don't like faking feedback unless the device is truly one-way. Is overriding ```FakeFeedbackForStandardCommand``` the right way to handle this?
### In file PanasonicTHXXCQ1UProtocol.cs
* I wanted to support power toggle so I overrode (overrided?) ```Power()``` but that somehow screwed up polling. The polling commands were hitting ```ValidateResponse``` with a completely different ```CommonCommandGroupType```. Any possible reason?
* A Mute On or Mute Off command will cause, during the next polling cycle, the polling command to be double prepared. Also, any reason?