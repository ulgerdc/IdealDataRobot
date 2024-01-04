using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography;
using System.Collections.Concurrent;






namespace ideal
{
    #region User  


    public class User
    {

        public void Deneme(dynamic Sistem) // idealde cağirmak için kullanacağiniz isim 
        {
            try
            {
                // kod buraya





                /// kod sonu

            }
            catch (Exception error)
            {
                string errorline = "\r\n" + "\r\n" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "\r\n" + "\r\n" +
                      "Message : " + error.Message + "\r\n" + "\r\n" +
                      "Source: " + error.Source + "\r\n" + "\r\n" +
                      "StackTrace : " + error.StackTrace + "\r\n";
                Sistem.AlgoAciklama = errorline;
            }
        }
        #endregion
    }

}