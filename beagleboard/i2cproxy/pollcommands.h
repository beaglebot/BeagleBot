#ifndef POLLTHREAD_H
#define POLLTHREAD_H

#include <stdbool.h>

void process_add_poll_command(const char *command, char *reply, int reply_size);
void process_remove_poll_command(const char *command, char *reply, int reply_size);
void start_poll_thread(int port, int bus, bool verbose);

#endif
