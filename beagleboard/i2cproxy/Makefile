CC = gcc
CFLAGS = -g
OBJECTS = i2cproxy.o ../common/i2c.o linereader.o commands.o prlist.o pollcommands.o \
		  ../common/utils.o ../common/network_utils.o

i2cproxy: $(OBJECTS)
	$(CC) $(CFLAGS) $(OBJECTS) -lrt -lpthread -pthread -lstdc++ -o i2cproxy

linereadertest: linereader.o linereadertest.o
	$(CC) $(CFLAGS) linereader.o linereadertest.o -o linereadertest

clean:
	rm -f i2cproxy
	rm *.o
	rm ../common/utils.o
	rm ../common/network_utils.o
