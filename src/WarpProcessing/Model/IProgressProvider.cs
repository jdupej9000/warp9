namespace Warp9.Model
{
    public interface IProgressProvider
    {
        void StartBatch(int numTasks);
        void StartTask(int taskIdx);
        void FinishTask(int taskIdx);
        void EndBatch();

    }
}
