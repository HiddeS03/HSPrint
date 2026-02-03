namespace HSPrint.Services;

public interface INetworkService
{
    Task<bool> PrintToRemoteNode(string targetIp, int targetPort, string printerName, string printType, string data);
    Task<Models.NetworkInfo?> GetRemoteNodeInfo(string targetIp, int targetPort);
}
