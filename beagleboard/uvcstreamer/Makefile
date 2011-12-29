CC = gcc
CFLAGS = -g
OBJECTS = uvcstreamer.o network.o webcam.o ../common/network_utils.o ../common/utils.o

uvcstreamer: $(OBJECTS)
	$(CC) $(CFLAGS) $(OBJECTS) -lm -lrt -o uvcstreamer

webcam.o: webcam.c webcam.h

network.o: network.c network.h

clean:
	rm -f uvcstreamer
	rm -f *.o
	rm -f ../common/network_utils.o
	rm -f ../ommon/utils.o
