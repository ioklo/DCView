using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Resources;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DCView.Lib
{
    public static class StorageUtil
    {
        static IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();

        static public bool MakeDir(string dir)
        {
            // 루트를 만들기 시도하면 무조건 성공
            if (dir == null) 
                return true;

            // 1. 지금 디렉토리가 있다면 바로 리턴
            if (isoStorage.DirectoryExists(dir))
                return true;            

            // 일단 부모 디렉토리 만들기를 하고 실패하면 더이상 시도하지 않고 false 리턴
            if (!MakeDir(Path.GetDirectoryName(dir)))
                return false;

            // 현재 디렉토리 만들어주고
            isoStorage.CreateDirectory(dir);
            return true;
        }
       

        // appPath의 리소스를 storagePath에 넣습니다.
        static public bool CopyResourceToStorage(string appPath, string storagePath, bool bForce = false)
        {
            StreamResourceInfo info = Application.GetResourceStream(new Uri(appPath, UriKind.Relative));
            if (info == null)
                return false;

            if (!bForce && isoStorage.FileExists(storagePath))
                return true;

            // storagePath에 디렉토리 자동생성
            if (!MakeDir(Path.GetDirectoryName(storagePath)))
                return false;            

            byte[] buffer = new byte[1024 * 32]; // 버퍼는 32kb

            using(var reader = new BinaryReader(info.Stream))
            using(var writer = new BinaryWriter(isoStorage.CreateFile(storagePath)))
            {
                int readBytes = 0;
                while ((readBytes = reader.Read(buffer, 0, buffer.Length)) != 0)
                    writer.Write(buffer, 0, readBytes);
            }

            return true;
        }

        
    }
}
