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

static void init_mmap(struct webcam *webcam)
{
	struct v4l2_requestbuffers req;
	int i;

	// Request memory mapping.	
	memset(&req, 0, sizeof(req));
	req.count = 4;
	req.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	req.memory = V4L2_MEMORY_MMAP;
	if (-1 == xioctl(webcam->fd, VIDIOC_REQBUFS, &req)) {
		if (EINVAL == errno) {
			fatal("%s does not support memory mapping", webcam->device_path);
		}
		fatal_errno("VIDIOC_REQBUFS");
	}
	if (req.count < 2) {
		fatal("Insufficient buffer memory on %s", webcam->device_path);
	}

	// Allocate the buffers.
	webcam->buffers = calloc(req.count, sizeof (*webcam->buffers));
	if (!webcam->buffers) {
		fatal("Out of memory");
	}

	// Do the memory mapping.
	for (i = 0; i < req.count; ++i) 
	{
		struct v4l2_buffer buf;
		memset(&buf, 0, sizeof(buf));
		buf.type        = V4L2_BUF_TYPE_VIDEO_CAPTURE;
		buf.memory      = V4L2_MEMORY_MMAP;
		buf.index       = i;
		if (-1 == xioctl (webcam->fd, VIDIOC_QUERYBUF, &buf)) {
			fatal_errno("VIDIOC_QUERYBUF");
		}

		webcam->buffers[i].length = buf.length;
		webcam->buffers[i].start = mmap(NULL, buf.length, PROT_READ | PROT_WRITE, MAP_SHARED, webcam->fd, buf.m.offset);
		if (MAP_FAILED == webcam->buffers[i].start) {
			fatal_errno("mmap");
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

void init_webcam(struct webcam *webcam)
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
		fatal("Cannot identify '%s': %d, %s", webcam->device_path, errno, strerror(errno));
	}

	if (!S_ISCHR (st.st_mode)) {
		fatal("%s is not a character device", webcam->device_path);
	}

	webcam->fd = open(webcam->device_path, O_RDWR | O_NONBLOCK, 0);
	if (-1 == webcam->fd) {
		fatal("Cannot open '%s': %d, %s", webcam->device_path, errno, strerror(errno));
	}

	// Check the webcam is capable of streaming video.
	if (-1 == xioctl (webcam->fd, VIDIOC_QUERYCAP, &cap)) {
		if (EINVAL == errno) {
			fatal("%s is not a V4L2 device", webcam->device_path);
		}
		fatal_errno("VIDIOC_QUERYCAP");
	}

	if (!(cap.capabilities & V4L2_CAP_VIDEO_CAPTURE)) {
		fatal("%s is no video capture device", webcam->device_path);
	}

	if (!(cap.capabilities & V4L2_CAP_STREAMING)) {
		fatal( "%s does not support streaming i/o", webcam->device_path);
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
		fatal("Pixel format not supported");
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
		fatal("Unable to set frame rate");
	}

	// Ask for the framerate, to make sure it was set correctly.
	ret = xioctl(webcam->fd, VIDIOC_G_PARM, &setfps);
	if (ret == 0) {
		float confirmed_fps = (float)setfps.parm.capture.timeperframe.denominator 
			/ (float)setfps.parm.capture.timeperframe.numerator;

		if (confirmed_fps != (float)n / (float)d) {
			fatal("Requested frame rate %g fps is not supported", webcam->framerate);
		}
	}
	else {
		fatal("Unable to read current frame rate");
	}

	// Set the quality.
	if (webcam->quality >= 0 && webcam->quality <= 100) {
		memset(&quality, 0, sizeof(quality));
		if (xioctl(webcam->fd, VIDIOC_G_JPEGCOMP, &quality) == -1) {
			fatal("Unable to get jpeg quality info");
		}
		quality.quality = webcam->quality;
		if (xioctl(webcam->fd, VIDIOC_S_JPEGCOMP, &quality) == -1) {
			fatal("Unable to set jpeg quality info");
		}
	}

	// Create the memory mapped buffers which will be used to received the frames.
	init_mmap(webcam);
	
	webcam->state = INITIALIZED;	
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
	if (-1 == res) {
		if (EINTR == errno) return;
		fatal_errno("select");
	}

	if (0 == res) {
		fprintf(stderr, "Timeout waiting for frame.");
		return;
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
				return;

			case EIO://EIO ignored

			default:
				fatal_errno("VIDIOC_DQBUF");
		}
	}
	assert(buf.index < webcam->numbuffers);

	res = 0;
	if (webcam->frames_already_skipped++ >= webcam->skip) {
		webcam->frames_already_skipped = 0;
		res = write_image(webcam->buffers[buf.index].start, webcam->buffers[buf.index].length, webcam, socket);
	}

	if (-1 == xioctl (webcam->fd, VIDIOC_QBUF, &buf)) {
		fatal_errno("VIDIOC_QBUF");
	}

	if (res == -1) return -1;

	return 0;
}

void start_capturing(struct webcam *webcam)
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
		if (-1 == xioctl (webcam->fd, VIDIOC_QBUF, &buf)) {
			fatal_errno("VIDIOC_QBUF");
		}
	}

	webcam->frames_already_skipped = INT_MAX;
	webcam->state = CAPTURING;

	type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	if (-1 == xioctl (webcam->fd, VIDIOC_STREAMON, &type)) {
		fatal_errno("VIDIOC_STREAMON");
	}
}

void stop_capturing(struct webcam *webcam)
{
	enum v4l2_buf_type type;
	type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	if (-1 == xioctl(webcam->fd, VIDIOC_STREAMOFF, &type)) {
		fatal_errno("VIDIOC_STREAMOFF");
	}

	webcam->state = INITIALIZED;
}

void close_webcam(struct webcam *webcam)
{
	int i;

	if (-1 == close(webcam->fd)) {
		fatal_errno("close");
	}

	for (i = 0; i < webcam->numbuffers; ++i) {
		if (-1 == munmap (webcam->buffers[i].start, webcam->buffers[i].length)) {
			fatal_errno("munmap");
		}
	}

	free(webcam->buffers);

	webcam->fd = -1;
	webcam->buffers = 0;
	webcam->numbuffers = 0;
	webcam->state = CLOSED;
}

