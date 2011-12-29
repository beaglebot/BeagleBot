#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <netinet/tcp.h>
#include "network_utils.h"

void *get_in_addr(struct sockaddr *sa)
{
	if (sa->sa_family == AF_INET) {
		return &(((struct sockaddr_in*)sa)->sin_addr);
	}
	return &(((struct sockaddr_in6*)sa)->sin6_addr);
}

int create_and_bind_tcp_socket(int port)
{
	char port_as_string[20];
	snprintf(port_as_string, sizeof(port_as_string), "%d", port);

	int rv;
	struct addrinfo hints, *localAddresses;
	memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_flags = AI_PASSIVE; // use my IP
	if ((rv = getaddrinfo(NULL, port_as_string, &hints, &localAddresses)) != 0) {
		fatal_errno("getaddrinfo");
    }

	// loop through all the results and bind to the first we can
	int yes = 1;
	int sock;
	struct addrinfo *localAddress;
	for(localAddress = localAddresses; localAddress != NULL; localAddress = localAddress->ai_next) {
		if ((sock = socket(localAddress->ai_family, localAddress->ai_socktype, localAddress->ai_protocol)) == -1) {
			continue;
		}

		if (setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, &yes, sizeof(yes)) == -1) {
			close(sock);
			continue;
		}

		if (setsockopt(sock, IPPROTO_TCP, TCP_NODELAY, &yes, sizeof(yes)) == -1) {
			continue;
		}
	
		if (bind(sock, localAddress->ai_addr, localAddress->ai_addrlen) == -1) {
			close(sock);
			continue;
		}

		break;
	}

	// All done with this structure.
	freeaddrinfo(localAddresses); 

	// Check if we succeeded.
	if (localAddress == NULL)  {
        fatal("Couldn't bind");
    }

	return sock;
}

void get_address_ip(struct sockaddr *address, char *ip, socklen_t ipSize)
{
	void *sin_addr;
	if (address->sa_family == AF_INET)
		sin_addr = &(((struct sockaddr_in*)address)->sin_addr);
	else
		sin_addr = &(((struct sockaddr_in6*)address)->sin6_addr);	
	inet_ntop(address->sa_family, sin_addr, ip, ipSize); 
}

int sendall(int sock, void *buffer, int count)
{
    int n, total = 0;

    while(total < count) {
        n = send(sock, buffer + total, count - total, MSG_NOSIGNAL);
        if (n == -1) { break; }
        total += n;
    }

    return n==-1?-1:0;
}

int create_and_connect_tcp_socket(char *client_ip, int port)
{
	int sock, numbytes;  
    struct addrinfo hints, *servinfo, *p;
    int rv, flag = 1;
    char s[INET6_ADDRSTRLEN];
	char portAsString[17];

	printf("Attempting to connect to %s on port %d\n", client_ip, port);
	memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
	snprintf(portAsString, 10, "%d", port);
    if ((rv = getaddrinfo(client_ip, portAsString, &hints, &servinfo)) != 0) {
        fprintf(stderr, "ERROR => Couldn't find client. The error was: %s\n", gai_strerror(rv));
        return -1;
    }

    // loop through all the results and connect to the first we can
    for(p = servinfo; p != NULL; p = p->ai_next) {
        if ((sock = socket(p->ai_family, p->ai_socktype, p->ai_protocol)) == -1) {
            perror("ERROR => Couldn't create socket. The error was");
            continue;
        }
		
		if (setsockopt(sock, IPPROTO_TCP, TCP_NODELAY, &flag, sizeof(flag))) {
			perror("ERROR -> Couldn't set setsockopt. The error was");
			continue;
		}

        if (connect(sock, p->ai_addr, p->ai_addrlen) == -1) {
            close(sock);
            perror("ERROR => Couldn't connect to client. The error was");
            continue;
        }

        break;
    }

    if (p == NULL) {
		fprintf(stderr, "ERROR => Couldn't connect.");
        return -1;
    }
	printf("Successfully opened poll connection.\n");
    freeaddrinfo(servinfo); 

	return sock;
}
