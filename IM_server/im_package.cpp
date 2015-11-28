#include "im_package.h"
#include <string.h>
#include "compiler_option.h"

#ifndef WINDOWS
#include <arpa/inet.h>
#else
#include <Winsock2.h>
#pragma comment(lib, "ws2_32.lib")
#endif

uint32_t im_package::preread()
{
	return ntohl(*((uint32_t *)(buffer + 96)));
}

void im_package::split()
{
	//copy head aera
	memcpy(&sender, buffer + 0, 4);
	memcpy(&type, buffer + 4, 4);
	memcpy(&information, buffer + 8, 4);
	memcpy(&receiver_number, buffer + 12, 4);
	memcpy(receiver, buffer + 16, 80);
	memcpy(&content_lenth, buffer + 96, 4);
	//ntoh head aera
	ntoh_head();
	//copy content aera
	memcpy(content, buffer + 100, content_lenth);
}

void im_package::build()
{
	//copy content area
	memcpy(buffer + 100, content, content_lenth);
	//hton head aera
	hton_head();
	//copy head aera
	memcpy(buffer + 0, &sender, 4);
	memcpy(buffer + 4, &type, 4);
	memcpy(buffer + 8, &information, 4);
	memcpy(buffer + 12, &receiver_number, 4);
	memcpy(buffer + 16, receiver, 80);
	memcpy(buffer + 96, &content_lenth, 4);
}

void im_package::ntoh_head()
{
	sender = ntohl(sender);
	type = ntohl(type);
	content_lenth = ntohl(content_lenth);
	information = ntohl(information);
	for (int i = 0; i < 20; ++i)
	{
		receiver[i] = ntohl(receiver[i]);
	}
	receiver_number = ntohl(receiver_number);
}

void im_package::hton_head()
{
	sender = htonl(sender);
	type = htonl(type);
	information = htonl(information);
	receiver_number = htonl(receiver_number);
	for (int i = 0; i < 20; ++i)
	{
		receiver[i] = htonl(receiver[i]);
	}
	content_lenth = htonl(content_lenth);
}

