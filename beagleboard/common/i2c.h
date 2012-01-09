#ifndef MYI2C_h
#define MYI2C_H

#include <stdbool.h>
#include <stdint.h>

int open_i2c(int bus, bool quiet); 
int read_i2c(int handle, uint8_t address, uint8_t reg, bool quiet);
int read_i2c_multiple(int handle, uint8_t address, uint8_t reg, int count, bool quiet, uint8_t *result);
int write_i2c(int handle, uint8_t address, uint8_t reg, uint8_t value, bool quiet);

#endif
