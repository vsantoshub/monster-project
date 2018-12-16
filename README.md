monster-project
<b> Real-time Thermal Stress Monitor </b>
<br />
This project was developed inside the Mechanical Engineering department in the Federal Univesity of Santa Catarina. <br />
Also this project played a critical role in a study of working condition inside poultry slaughterhouses in South Brazil. <br />
<br />
Some technologies used are listed below. <br />
<br />
Hardware: <br />
+ Xbee S3B radio module <br />
+ PCB specially designed to be as smaller as possible  <br />
+ Murata NXR NTX thermistors  <br />
+ Lithim-Ion battery powered modules (two months duration)  <br />

User-Interface: <br />
+ C# interface developed with Windows Forms framework <br />
+ The interface is programmatically built  <br />
+ Used Zigbee's proprietary API to communicate with the radios <br />
<br /><br />

<b>User Interface </b>
<br />
The project can be built normally using Microsoft Visual Studio. No external dependencies needed.
<br />
You will need to setup some specific dependencies such as:
<br />
The user interface initially makes a search, to look for the radios registered in the network.
<br />
After this, the interface shows the temperature measurements, updated in each five minutes. <br />
The Zigbee radios are configured in the sleep mode, waking up every five minutes to sample the temperature. <br />