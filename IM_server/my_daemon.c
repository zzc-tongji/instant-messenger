#include "compiler_option.h"

#ifndef WINDOWS
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>

int my_daemon(int chg_dir)
{
	pid_t pid;
	int i;

	//Step 1: Create a child process called TEMP
	pid = fork();
	if (pid > 0)
	{
		//Parent process : Terminate
		exit(0);
	}
	else if (pid < 0)
	{
		//TEMP process : Fail to create a child process
		return -1;
	}
	//TEMP process
	//Step 2: Create a session and set the session ID and the process group ID as this process ID
	if (setsid() < 0)
	{
		return -2;
	}
	//Step 3: Create a child process called PURPOSE
	//The PURPOSE process is not the session leader and the group leader, as we expect.
	pid = fork();
	if (pid > 0)
	{
		//TEMP process : Terminate
		exit(0);
	}
	else if (pid < 0)
	{
		//TEMP process : Fail to create a child process again
		return -3;
	}
	//Step 4: Close all file which is open
	for (i = 0; i < getdtablesize(); ++i)
	{
		close(i);
	}
	if (chg_dir)
	{
		//Step 5: Change working directory
		if (chdir("/tmp") < 0)
		{
			return -5;
		}
	}
	//Step 6: Set file mode creation mask
	umask(0);
	return 1;
}
#endif

