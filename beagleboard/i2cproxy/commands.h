#ifndef COMMANDS_H
#define COMMANDS_H

void process_ping_command(const char *command, char *reply, int reply_size);
void process_get_command(const char *command, int i2c_handle, char *reply, int reply_size);
void process_set_command(const char *command, int i2c_handle, char *reply, int reply_size);
void process_help(const char *command, char *reply, int reply_size);

void read_i2c_multiple_as_string(int i2c_handle, uint8_t address, uint8_t reg, int count, char *result, int result_size);
	
#endif
