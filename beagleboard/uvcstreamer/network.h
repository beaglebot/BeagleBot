#ifndef NETWORK_H
#define NETWORK_H

int create_udp_socket(char *target, char *port);
int create_tcp_socket(char *port);
int write_image(char *buffer, int size, int sockfd);
void *get_in_addr(struct sockaddr *sa);

#endif
