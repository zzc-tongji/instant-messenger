using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IM
{
    public class User
    {
        /*      对象：字段      */

        Int32 _username;
        String _password;

        /*      对象：属性      */

        public Int32 Username
        {
            get { return _username; }
            set { _username = value; }
        }
        public String Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /*      对象：构造与析构方法      */

        //构造方法（无参数）
        public User()
        {

        }
    }
}
