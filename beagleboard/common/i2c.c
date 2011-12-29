#include <linux/errno.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <stdint.h>
#include <unistd.h>
#include <pthread.h>
#include <linux/i2c-dev.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include "i2c.h"

// It looks like bad things happen if you attempt to read and write to the same I2C bus
// at the same time, even through different handles.
pthread_mutex_t i2c_mutex = PTHREAD_MUTEX_INITIALIZER;

int open_i2c(int bus, bool quiet) {
	char devicePath[20];
	int i2cHandle;

	sprintf(devicePath,"/dev/i2c-%d", bus);

 	if (!quiet) printf("Opening i2c device %s\n", devicePath);
	if ((i2cHandle = open(devicePath, O_RDWR)) < 0) {
		if (!quiet) perror("ERROR => Failed to open the i2c device. The error was:");
		return -1;	
	}

	if (!quiet) printf("Using I2C device handle %d\n", i2cHandle);
	return i2cHandle;
}

int read_i2c(int handle, uint8_t address, uint8_t reg, bool quiet) {
	uint8_t buf[1];

	if (!quiet) printf("Reading from I2C at address %d, register=%d\n", address, reg);

	pthread_mutex_lock(&i2c_mutex);

	if (ioctl(handle, I2C_SLAVE, address) < 0) {
		if (!quiet) perror("Failed setting slave address"); 
		pthread_mutex_unlock(&i2c_mutex);
		return -1;
	}

	buf[0] = reg;
	if (!quiet) printf("  Writing register %d to I2C bus\n", reg);
	if (write(handle, buf, 1) != 1) {
		if (!quiet) perror("Failed to write register to I2C bus"); 
		pthread_mutex_unlock(&i2c_mutex);
		return -1;
	}

	if (!quiet) printf("  Reading register value from I2C bus\n");
	if (read(handle, buf, 1) != 1) {
		if (!quiet) perror("Failed to read value from the I2C bus"); 
		pthread_mutex_unlock(&i2c_mutex);
		return -1;
	}
	pthread_mutex_unlock(&i2c_mutex);

	if (!quiet) printf("  Read i2c value %d\n", buf[0]);

	return buf[0];
}

int read_i2c_multiple(int handle, uint8_t address, uint8_t reg, int count, bool quiet, uint8_t *result) {
	uint8_t buf[1];

	if (!quiet) printf("Reading from I2C at address %d, register=%d\n", address, reg);

	pthread_mutex_lock(&i2c_mutex);

	if (ioctl(handle, I2C_SLAVE, address) < 0) {
		if (!quiet) perror("Failed setting slave address"); 
		pthread_mutex_unlock(&i2c_mutex);
		return -1;
	}

	buf[0] = reg;
	if (!quiet) printf("  Writing register %d to I2C bus\n", reg);
	if (write(handle, buf, 1) != 1) {
		if (!quiet) perror("Failed to write register to I2C bus"); 
		pthread_mutex_unlock(&i2c_mutex);
		return -1;
	}

	if (!quiet) printf("  Reading register values from I2C bus\n");
	if (read(handle, result, count) != count) {
		if (!quiet) perror("Failed to read value from the I2C bus"); 
		pthread_mutex_unlock(&i2c_mutex);
		return -1;
	}
	pthread_mutex_unlock(&i2c_mutex);

	return 0;
}

int write_i2c(int file, uint8_t address, uint8_t reg, uint8_t value, bool quiet) {
	uint8_t buf[2];

	if (!quiet) printf("Writing value %d to I2C at address=%d, register=%d\n", value, address, reg);
	if (!quiet) printf("  Setting slave address to %d\n", address);

	pthread_mutex_lock(&i2c_mutex);

	if (ioctl(file, I2C_SLAVE, address) < 0) {
		pthread_mutex_unlock(&i2c_mutex);
		if (!quiet) perror("ERROR: Failed setting slave address\n"); 
		return -1;
	}

	if (!quiet) printf("  Writing register %d and value %d to I2C bus\n", reg, value);
	buf[0] = reg;
	buf[1] = value;
	if (write(file, buf, 2) != 2) {
		pthread_mutex_unlock(&i2c_mutex);
		if (!quiet) perror("ERROR:Failed to write register and value to the I2C bus\n"); 
		return -1;
	}

	pthread_mutex_unlock(&i2c_mutex);
	
	if (!quiet) printf("  Write successful\n");

	return 0;
}

