#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <float.h>
#include <fcntl.h>
#include <limits.h>
#include <unistd.h>
#include <errno.h>
#include <math.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/ioctl.h>
#include <asm/types.h>
#include <linux/videodev2.h>
#include "webcam.h"
#include "../common/utils.h"

struct image_buffer {    
	void *start;
	size_t length;
};

static int xioctl(int fd, int request, void *arg)
{
	int r;

	do r = ioctl (fd, request, arg);
	while (-1 == r && EINTR == errno);

	return r;
}

static int init_mmap(struct webcam *webcam)
{
	struct v4l2_requestbuffers req;
	int i;

	// Request memory mapping.	
	memset(&req, 0, sizeof(req));
	req.count = 4;
	req.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	req.memory = V4L2_MEMORY_MMAP;
	if (xioctl(webcam->fd, VIDIOC_REQBUFS, &req) == -1) {
		if (errno == EINVAL) {
			fprintf(stderr, "ERROR => %s does not support memory mapping. The error was: %s\n", 
					webcam->device_path, strerror(errno));
			return -1;
		}
		fprintf(stderr, "ERROR => Error calling REQBUFS on %s. The error was: %s\n", 
				webcam->device_path, strerror(errno));
		return -1;
	}
	if (req.count < 2) {
		fprintf(stderr, "ERROR => Insufficient buffer memory on %s\n", webcam->device_path);
		return -1;
	}

	// Allocate the buffers.
	webcam->buffers = calloc(req.count, sizeof (*webcam->buffers));
	if (!webcam->buffers) {
		fprintf(stderr, "ERROR => Out of memory.\n");
		return -1;
	}

	// Do the memory mapping.
	for (i = 0; i < req.count; ++i) 
	{
		struct v4l2_buffer buf;
		memset(&buf, 0, sizeof(buf));
		buf.type        = V4L2_BUF_TYPE_VIDEO_CAPTURE;
		buf.memory      = V4L2_MEMORY_MMAP;
		buf.index       = i;
		if (xioctl (webcam->fd, VIDIOC_QUERYBUF, &buf) == -1) {
			perror("ERROR => Error calling VIDIOC_QUERYBUF. The error was: ");
			return -1;
		}

		webcam->buffers[i].length = buf.length;
		webcam->buffers[i].start = mmap(NULL, buf.length, PROT_READ | PROT_WRITE, MAP_SHARED, webcam->fd, buf.m.offset);
		if (webcam->buffers[i].start == MAP_FAILED) {
			perror("ERROR => Error doing memory mapping. The error was: ");
			return -1;
		}
	}

	webcam->numbuffers = req.count;
}

static int float_to_fraction_recursive(double f, double p, int *num, int *den)
{
	int whole = (int)f;
	f = fabs(f - whole);

	if(f > p) {
		int n, d;
		int a = float_to_fraction_recursive(1 / f, p + p / f, &n, &d);
		*num = d;
		*den = d * a + n;
	}
	else {
		*num = 0;
		*den = 1;
	}
	return whole;
}

static void float_to_fraction(float f, int *num, int *den)
{
	int whole = float_to_fraction_recursive(f, FLT_EPSILON, num, den);
	*num += whole * *den;
}

int init_webcam(struct webcam *webcam)
{
	int n, d;
	struct v4l2_capability cap;
	struct v4l2_cropcap cropcap;
	struct v4l2_crop crop;
	struct v4l2_format fmt;
	struct v4l2_streamparm setfps;  
	struct v4l2_jpegcompression quality;
	struct stat st;

	// Check the device path looks sane.
	if (-1 == stat(webcam->device_path, &st)) {
		fprintf(stderr, "ERROR => Cannot identify '%s': %d, %s\n", webcam->device_path, errno, strerror(errno));
		return -1;
	}

	if (!S_ISCHR (st.st_mode)) {
		fprintf(stderr, "ERROR => %s is not a character device\n", webcam->device_path);
		return -1;
	}

	webcam->fd = open(webcam->device_path, O_RDWR | O_NONBLOCK, 0);
	if (-1 == webcam->fd) {
		fprintf(stderr, "ERROR => Cannot open '%s': %d, %s\n", webcam->device_path, errno, strerror(errno));
		return -1;
	}

	// Check the webcam is capable of streaming video.
	if (-1 == xioctl (webcam->fd, VIDIOC_QUERYCAP, &cap)) {
		if (EINVAL == errno) {
			fprintf(stderr, "ERROR => %s is not a V4L2 device\n", webcam->device_path);
			return -1;
		}
		perror("ERROR => Error calling VIDIOC_QUERYCAP. The error was: ");
		return -1;
	}

	if (!(cap.capabilities & V4L2_CAP_VIDEO_CAPTURE)) {
		fprintf(stderr, "ERROR => %s is no video capture device.\n", webcam->device_path);
		return -1;
	}

	if (!(cap.capabilities & V4L2_CAP_STREAMING)) {
		fprintf(stderr, "ERROR => %s does not support streaming i/o\n", webcam->device_path);
		return -1;
	}

	// Work out the pixel format.
	int format = v4l2_fourcc(
			webcam->format[0], 
			webcam->format[1], 
			webcam->format[2], 
			webcam->format[3]);

	// Configure the device.
	memset(&fmt, 0, sizeof(fmt));
	fmt.type                = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	fmt.fmt.pix.width       = webcam->width;
	fmt.fmt.pix.height      = webcam->height;
	fmt.fmt.pix.pixelformat = format; 
	if (-1 == xioctl(webcam->fd, VIDIOC_S_FMT, &fmt)) {
		perror("ERROR => Pixel format not supported. The error was: ");
		return -1;
	}
	webcam->size = fmt.fmt.pix.sizeimage;

	// Set the frame rate.
	memset(&setfps, 0, sizeof(setfps));
	setfps.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	float_to_fraction(webcam->framerate, &n, &d);
	setfps.parm.capture.timeperframe.numerator = d;
	setfps.parm.capture.timeperframe.denominator = n;
	int ret = xioctl(webcam->fd, VIDIOC_S_PARM, &setfps);
	if(ret == -1) {
		perror("ERROR => Unable to set frame rate. The error was: ");
		return -1;
	}

	// Ask for the framerate, to make sure it was set correctly.
	ret = xioctl(webcam->fd, VIDIOC_G_PARM, &setfps);
	if (ret == 0) {
		float confirmed_fps = (float)setfps.parm.capture.timeperframe.denominator 
			/ (float)setfps.parm.capture.timeperframe.numerator;

		if (confirmed_fps != (float)n / (float)d) {
			fprintf(stderr, "ERROR => Requested frame rate %g fps is not supported\n", webcam->framerate);
		}
	}
	else {
		perror("ERROR => Unable to read current frame rate. The error was: ");
	}

	// Set the quality.
	if (webcam->quality >= 0 && webcam->quality <= 100) {
		memset(&quality, 0, sizeof(quality));
		if (xioctl(webcam->fd, VIDIOC_G_JPEGCOMP, &quality) == -1) {
			perror("ERROR => Unable to get jpeg quality info. The error was: ");
		}
		quality.quality = webcam->quality;
		if (xioctl(webcam->fd, VIDIOC_S_JPEGCOMP, &quality) == -1) {
			perror("ERROR => Unable to set jpeg quality info. The error was: ");
		}
	}

	// Create the memory mapped buffers which will be used to received the frames.
	ret = init_mmap(webcam);
	if (ret == -1) return -1;
	
	webcam->state = INITIALIZED;

	return 0;
}

int process_frame(struct webcam *webcam, int socket)
{
	struct v4l2_buffer buf;
	fd_set fds;
	struct timeval tv;
	int res;

	assert(webcam->state == CAPTURING);

	// Wait for a new frame.
	FD_ZERO(&fds);
	FD_SET(webcam->fd, &fds);
	tv.tv_sec = 2;
	tv.tv_usec = 0;
	res = select(webcam->fd + 1, &fds, NULL, NULL, &tv);
	if (res == -1) {
		if (EINTR == errno) return 0;
		perror("ERROR => Error calling select. The error was ");
		return -1;
	}

	if (res == 0) {
		fprintf(stderr, "ERROR => Timeout waiting for frame.\n");
		return 0;
	}

	// Grab the buffer with the new frame.
	memset(&buf, 0, sizeof(buf));
	buf.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	buf.memory = V4L2_MEMORY_MMAP;
	if (-1 == xioctl(webcam->fd, VIDIOC_DQBUF, &buf)) 
	{
		switch (errno) 
		{
			case EAGAIN:
				return 0;

			default:
				perror("ERROR => Error calling VIDIOC_DQBUF. The error was ");
				return -1;
		}
	}
	assert(buf.index < webcam->numbuffers);

	res = 0;
	if (webcam->frames_already_skipped++ >= webcam->skip) {
		webcam->frames_already_skipped = 0;
		res = write_image(webcam->buffers[buf.index].start, webcam->buffers[buf.index].length, webcam, socket);
	}

	if (xioctl (webcam->fd, VIDIOC_QBUF, &buf) == -1) {
		perror("ERROR => Error calling VIDIOC_QBUF. The error was ");
		return -1;
	}

	if (res == -1) return -1;

	return 0;
}

int start_capturing(struct webcam *webcam)
{
	enum v4l2_buf_type type;
	int i;

	for (i = 0; i < webcam->numbuffers; ++i) 
	{
		struct v4l2_buffer buf;
		memset(&buf, 0, sizeof(buf));
		buf.type        = V4L2_BUF_TYPE_VIDEO_CAPTURE;
		buf.memory      = V4L2_MEMORY_MMAP;
		buf.index       = i;
		if (xioctl (webcam->fd, VIDIOC_QBUF, &buf) == -1) {
			perror("ERROR => Error calling VIDIOC_QBUF. Error was: ");
			return -1;
		}
	}

	webcam->frames_already_skipped = INT_MAX;
	webcam->state = CAPTURING;

	type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	if (-1 == xioctl (webcam->fd, VIDIOC_STREAMON, &type)) {
		perror("ERROR => Error calling VIDIOC_STREAMON. The error was: ");
		return -1;
	}

	return 0;
}

int stop_capturing(struct webcam *webcam)
{
	enum v4l2_buf_type type;
	type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	if (-1 == xioctl(webcam->fd, VIDIOC_STREAMOFF, &type)) {
		perror("ERROR => Error calling VIDIOC_STREAMOFF. The error was: ");
		return -1;
	}

	webcam->state = INITIALIZED;

	return 0;
}

void close_webcam(struct webcam *webcam)
{
	int i;

	for (i = 0; i < webcam->numbuffers; ++i) {
		if (-1 == munmap (webcam->buffers[i].start, webcam->buffers[i].length)) {
			perror("ERROR => Error calling munmap. The error was: ");
		}
	}

	free(webcam->buffers);

	if (close(webcam->fd) == -1) {
		perror("ERROR -> Error closing file. The error was: ");
	}

	webcam->fd = -1;
	webcam->buffers = 0;
	webcam->numbuffers = 0;
	webcam->state = CLOSED;
}

