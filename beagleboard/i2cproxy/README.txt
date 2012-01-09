I2CPROXY
========

Copyright (C) 2012 Ben Galvin

i2cproxy exposes a machine's I2C bus over TCP/IP, allowing a remote machine to
read and write to the I2C bus.

Once started the application listens on two ports, the command port (whose port
number is specified on the command line), and the poll port (the command port
number + 1). The command port is used to read and write values to the bus
synchronously. The poll port is used to return values which the client has asked
to be polled asynchronously.

See
http://yetanotherhackersblog.wordpress.com/2012/01/03/beaglebot-a-beagleboard-based-robot/


LICENSE
=======

This program is free software; you can redistribute it and/or modify it under
the terms of the GNU General Public License as published by the Free Software
Foundation; version 2 of the License.

This program is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE.  See the GNU General  Public License for more details.

You should have received a copy of the GNU General Public License along with
this program; if not, write to the Free Software Foundation, Inc., 59 Temple
Place, Suite 330, Boston, MA  02111-1307  USA



COMMAND LINE USAGE
==================

i2cproxy -p <port> -b <bus> [-v] [-d] [-l path]

where <port> is the port number which the application will listen on
      <bus> is the number of the i2c bus
      -v indicates that all requests and responses should be logged
      -d indicates it should run as a daemon
      -l indicates the path the log should be saved to if run as a daemon
         Defaults to /var/log/i2cproxy.log



NETWORK COMMANDS
================

Once the program is running you can connect to the command port and issue
commands. To test this out you can use netcat or telnet. The available commands
are:


GET
===

Reads the value of one I2C register or a set of sequential I2C registers.

Syntax: get <i2c address> <register> [num registers]

where <i2c address> is the i2c address (a number from 0-127)
      <register> is the i2c register number (a number from 0-255)
      [num registers] is an optional number of registers to read sequentially. 
      This defaults to 1.

Returns the value read from the I2C bus (a number between 0 and 255). If
multiple registers are requested, the individual values are seperated by spaces.
If there is an error, the text 'ERROR' is returned.


SET
===

Writes a value to one I2C register.

Syntax: set <i2c address> <register> <value>

where <i2c address> is the i2c address (a number from 0-127)
      <register> is the i2c register number (a number from 0-255)
	  <value> is the value to write (a number from 0-255)
	  
Returns 'OK' if the write was successful, or 'ERROR' otherwise. A more detailed
error description is avaible on the console (or log file if running in daemon
mode).


ADDPOLL
=======

While its possible to poll registers using just the get command, and some
polling logic in the client, this isn't ideal because of the extra network
traffic, latency, and variability the extra network hop adds to the poll times.
Instead, the addpoll command requests that i2cproxy poll a register (or set of
sequential registers) repeatedly with a given frequency, writing the results out
to the poll port.

Syntax: addpoll <delay in ms> <i2c address> <register> [num registers]

where <delay in ms> is the amount of time to wait between polls (in milliseconds)
      <i2c address> is the i2c address (a number from 0-127)
      <register> is the i2c register number (a number from 0-255)
      [num registers] is an optional number of registers to read sequentially. 
      This defaults to 1.

Returns a handle for the request (a 32 bit unsigned integer) if successful, or
'ERROR' otherwise. The handle is used to identify a poll result, and to stop
polling. Once the command has executed successfully, the program will begin
polling the requested registers, and periodically write the values of the
registers to the poll port. The values will be written in the format:
 
<poll handle>: <value1> [value 2] ... [value n]

where <poll handle> is the number returned by the addpoll command
      <value> is the value of the requested register. If multiple registers are 
      requested, the individual values are seperated by spaces. If there is an 
      error, the text 'ERROR' is returned.
      
 
RMPOLL
======
 
Stops a previously added poll request.

Syntax: rmpoll <poll handle>

where <poll handle> is the number returned by the addpoll command.

Returns 'OK' if the poll was stopped succesfully, or 'ERROR' otherwise.


PING
====

Used to check i2cproxy is still working correctly.

Syntax: ping

Returns 'OK'
