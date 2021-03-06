using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using SuperSocket.Ftp.FtpService.Storage;
using SuperSocket.Common;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace SuperSocket.Ftp.FtpService.Command
{
    public class STOR : FtpCommandBase
    {
        #region StringCommandBase<FtpSession> Members

        public override void ExecuteCommand(FtpSession session, StringRequestInfo requestInfo)
        {
            if (!session.Logged)
                return;

            string filename = requestInfo.Body;

            if (string.IsNullOrEmpty(filename))
            {
                session.SendParameterError();
                return;
            }

            if (!session.Context.User.IncreaseConnection())
            {
                session.Send(FtpCoreResource.ReachedLoginLimit_421);
                return;
            }

            DataConnection dataConn = session.CreateDataConnection();

            if (dataConn.RunDataConnection())
            {
                Stream stream = dataConn.GetStream(session.Context);

                try
                {
                    session.Send(FtpCoreResource.DataConnectionAccepted_150);

                    if (session.AppServer.FtpServiceProvider.StoreFile(session.Context, filename, stream))
                        session.Send(FtpCoreResource.DataTransferComplete_226);
                    else
                        session.Send(session.Context.Message);
                }
                catch (SocketException)
                {
                    session.Send(FtpCoreResource.DataConnectionError_426);
                }
                catch (Exception e)
                {
                    session.Logger.Error(e);
                    session.Send(FtpCoreResource.OuputFileError_551);
                }
                finally
                {
                    session.Context.User.DecreaseConnection();
                    session.CloseDataConnection(dataConn);
                }
            }
        }

        #endregion
    }
}
