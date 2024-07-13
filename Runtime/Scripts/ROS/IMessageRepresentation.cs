using Unity.Robotics.ROSTCPConnector.MessageGeneration;

public interface IMessageRepresentation<MessageType, RepresentationType> where MessageType : Message where RepresentationType : IMessageRepresentation<MessageType, RepresentationType>
{
    MessageType ToMessage();
    RepresentationType FromMessage(MessageType message);
}