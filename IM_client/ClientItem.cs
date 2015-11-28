using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IM
{
    public class ClientItem
    {
        /*      对象：字段      */

        Int32 _usernameInt32;
        String _usernameString;
        Boolean _online;
        Boolean _doudizhuOnline;

        /*      对象：属性      */

        public Int32 UsernameInt32
        {
            get { return _usernameInt32; }
            set { _usernameInt32 = value; }
        }

        public String UsernameString
        {
            get { return _usernameString; }
            set { _usernameString = value; }
        }

        public Boolean Online
        {
            get { return _online; }
            set { _online = value; }
        }

        public Boolean DoudizhuOnline
        {
            get { return _doudizhuOnline; }
            set { _doudizhuOnline = value; }
        }

        /*      对象：构造与析构方法      */

        //构造方法（3个参数）
        public ClientItem(Int32 usernameInt32, Boolean online, Boolean doudizhuOnline)
        {
            _usernameInt32 = usernameInt32;
            _usernameString = usernameInt32.ToString();
            _online = online;
            _doudizhuOnline = doudizhuOnline;
        }
    }
}
