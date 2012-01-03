UVCSTREAMER
===========

Copyright (C) 2012 Ben Galvin

Based on code from the V4L2 sample: 
http://v4l2spec.bytesex.org/spec/capture-example.html

Streams raw or MJPEG compressed frames from a UVC webcam over TCP/IP. The
stream consists of a sequence of images, where an image is defined as an 
image header followed by the raw image data. The image header is defined 
in network.c.

See
http://yetanotherhackersblog.wordpress.com/2012/01/02/beaglebot-a-board-based-robot/




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
