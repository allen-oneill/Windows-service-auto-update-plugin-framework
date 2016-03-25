# Windows service auto-update plugin framework

Sample code for article:

Windows service auto-update plugin framework

Published at: http://www.codeproject.com/Articles/700435/Windows-service-auto-update-plugin-framework

Introduction

Windows services are long running processes that operate in the background, however, just because they are out of the way does not mean they don't need to be updated. One of the problems with services is that they need admin access to install and reinstall when code needs to be updated. This article describes a framework that allows you to update the code in your windows service on demand, without the need for admin intervention. The code given works and a more integrated version is in production - the article here presents the general overview. There are many tweaks and different approaches that could be taken to implement the detail, depending on your particular requirements. Hopefully this will give your particular solution a head-start.  I encourage you to download the code, and if you have any improvements please leave a comment and send them on so everyone can benefit. The methodology presented here is very rough at the moment and leaves a lot of internal adaptation up to the developer. In a future release I will wrap the code together into a more robust self managing framework that can be installed as a package, that includes some other interesting stuff I am working on at the moment! 
