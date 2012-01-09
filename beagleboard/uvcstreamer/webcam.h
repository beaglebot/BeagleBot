#ifndef WEBCAM_H
#define WEBCAM_H

enum webcam_state
{
	NOT_INITIALIZED,
	INITIALIZED,
	CAPTURING,
	CLOSED
};

struct image_buffer;

struct webcam
{
	char device_path[50];
	int width, height;
	double framerate;
	char format[5];
	int quality;
	int skip;

	enum webcam_state state;
	
	int fd;
	int numbuffers;
	struct image_buffer *buffers;
	int size;
	int frames_already_skipped;
};

int init_webcam(struct webcam *webcam);
int start_capturing (struct webcam *webcam);
int process_frame(struct webcam *webcam, int socket);
int stop_capturing (struct webcam *webcam);
void close_webcam(struct webcam *webcam);

#endif
