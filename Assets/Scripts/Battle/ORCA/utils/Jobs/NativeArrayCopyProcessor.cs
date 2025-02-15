﻿using Unity.Collections;

namespace Nebukam.JobAssist
{
    public class NativeArrayCopyProcessor<T> : Processor<NativeArrayCopyJob<T>>
        where T : struct
    {
        protected NativeArray<T> m_outputArray = new NativeArray<T>(0, Allocator.Persistent);

        public NativeArray<T> inputArray { get; set; }
        public NativeArray<T> outputArray { get { return m_outputArray; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override void Prepare(ref NativeArrayCopyJob<T> job, float delta)
        {

            int length = inputArray.Length;
            if (m_outputArray.Length != length)
            {
                m_outputArray.Dispose();
                m_outputArray = new NativeArray<T>(length, Allocator.Persistent);
            }

            job.inputArray = inputArray;
            job.outputArray = outputArray;

            //return length;

        }

        protected override void Apply(ref NativeArrayCopyJob<T> job)
        {

        }
    }
}
