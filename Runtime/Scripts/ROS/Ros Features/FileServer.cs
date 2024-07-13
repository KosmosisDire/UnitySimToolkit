using System.Threading.Tasks;
using RosMessageTypes.FileServer;

public class FileServer : RosFeatureSingleton<FileServer>
{
    const string GET_FILE = "file_server/get_file";
    const string SAVE_FILE = "file_server/save_file";
    const string GET_FILE_STAT = "file_server/get_file_stat";


    protected override async Task Init()
    {
        await base.Init();
        ROS.RegisterRosService<GetBinaryFileRequest, GetBinaryFileResponse>(GET_FILE);
        ROS.RegisterRosService<SaveBinaryFileRequest, SaveBinaryFileResponse>(SAVE_FILE);
        ROS.RegisterRosService<GetFileStatRequest, GetFileStatResponse>(GET_FILE_STAT);
    }

    public static async Task<byte[]> GetFile(string name)
    {
        var response = (await ROS.SendServiceMessage<GetBinaryFileResponse>(GET_FILE, new GetBinaryFileRequest(name))).value;
        return response;
    }

    public static async Task SaveFile(string name, byte[] data)
    {
        await ROS.SendServiceMessage<SaveBinaryFileResponse>(SAVE_FILE, new SaveBinaryFileRequest(name, data));
    }

    public static async Task<GetFileStatResponse> GetFileStat(string name)
    {
        return await ROS.SendServiceMessage<GetFileStatResponse>(GET_FILE_STAT, new GetFileStatRequest(name));
    }

}