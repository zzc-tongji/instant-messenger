using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IM
{
    public class FileTransport
    {
        /*      类：字段      */

        public const Int32 _PeerNone = -2;

        /*      对象：字段      */

        Int32 _peerUsername;
        String _receiveFilePath;
        FileStream _receiveFileStream;
        String _sendFilePath;
        FileStream _sendFileStream;
        FileInfo _fileInfo;
        Int64 _fileSizeAll;
        Int64 _fileSizeNow;

        /*      对象：属性      */

        public Int32 PeerUsername
        {
            get { return _peerUsername; }
            set { _peerUsername = value; }
        }

        public String ReceiveFilePath
        {
            get { return _receiveFilePath; }
            set { ReceiveFilePath = value; }
        }

        public FileStream ReceiveFileStream
        {
            get { return _receiveFileStream; }
        }

        public String SendFilePath
        {
            get { return _sendFilePath; }
            set { SendFilePath = value; }
        }

        public FileStream SendFileStream
        {
            get { return _sendFileStream; }
        }

        public FileInfo _FileInfo
        {
            get { return _fileInfo; }
            set { _fileInfo = value; }
        }

        public Int64 FileSizeAll
        {
            get { return _fileSizeAll; }
            set { _fileSizeAll = value; }
        }

        public Int64 FileSizeNow
        {
            get { return _fileSizeNow; }
            set { _fileSizeNow = value; }
        }

        /*      对象：构造与析构方法      */

        //构造方法（无参数）
        public FileTransport()
        {
            _peerUsername = FileTransport._PeerNone;
            _sendFilePath = null;
            _sendFileStream = null;
            _receiveFilePath = null;
            _receiveFileStream = null;
            _fileInfo = null;
            _fileSizeAll = 0;
            _fileSizeNow = 0;
        }

        /*      对象：功能方法      */

        public Boolean ReceiveEnable(String receiveFilePath)
        {
            _receiveFilePath = receiveFilePath;
            try
            {
                _receiveFileStream = new FileStream(_receiveFilePath, FileMode.Create, FileAccess.Write);
                _fileInfo = new FileInfo(receiveFilePath);
            }
            catch (Exception)
            {
                return false;
            }
            _fileSizeAll = 0;
            _fileSizeNow = 0;
            return true;
        }

        public void ReceiveDisable(Boolean delete)
        {
            try
            {
                _receiveFileStream.Close();
            }
            catch (NullReferenceException)
            {

            }
            _receiveFileStream = null;
            if (delete)
            {
                File.Delete(_receiveFilePath);
            }
            _receiveFilePath = null;
            _fileInfo = null;
            _fileSizeAll = 0;
            _fileSizeNow = 0;
        }

        public Boolean SendEnable(String sendFilePath)
        {
            _sendFilePath = sendFilePath;
            try
            {
                _sendFileStream = new FileStream(_sendFilePath, FileMode.Open, FileAccess.Read);
                _fileInfo = new FileInfo(sendFilePath);
            }
            catch (Exception)
            {
                return false;
            }
            _fileSizeAll = _fileInfo.Length;
            _fileSizeNow = 0;
            return true;
        }

        public void SendDisable()
        {
            try
            {
                _sendFileStream.Close();
            }
            catch (NullReferenceException)
            {

            }
            _sendFileStream = null;
            _sendFilePath = null;
            _fileInfo = null;
            _fileSizeAll = 0;
            _fileSizeNow = 0;
        }
    }
}
