#ifndef NETWORK_UTILS_H
#define NETWORK_UTILS_H

int create_and_bind_tcp_socket(int port);
int create_and_connect_tcp_socket(char *client_ip, int port);
int sendall(int sock, void *buffer, int count);
void get_address_ip(struct sockaddr *address, char *ip, socklen_t ipSize);

#endif
