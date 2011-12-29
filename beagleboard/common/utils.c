#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>
#include <stdarg.h>
#include <time.h>

long get_time_in_ms() 
{
	struct timespec ts;
	clock_gettime(CLOCK_MONOTONIC, &ts);
	return (long)(ts.tv_sec * 1000 + ts.tv_nsec / 1000000);
}

void fatal(const char *message,...)
{
	va_list argp;
	va_start(argp, message);
	fprintf(stderr, "ERROR: ");
	vfprintf(stderr, message, argp);
	fprintf(stderr, "\n");
	va_end(argp);
	exit(EXIT_FAILURE);
}

void fatal_errno(const char *message)
{
	fprintf(stderr, "%s error %d, %s", message, errno, strerror(errno));
	exit(EXIT_FAILURE);
}
