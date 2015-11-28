#ifndef COMPILER_OPTION_H_
#define COMPILER_OPTION_H_

//compile in Windows
#define WINDOWS

//need to be daemon process
#ifndef WINDOWS
//#define DAEMON
#endif

//need to display log in console
#ifndef DAEMON
#define DISPLAY
#endif

/*
//pipe
#ifndef WINDOWS
#define PIPE
#endif
*/

#endif

