using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace EFM
{
    // Script created by unity forums user Sundersoft

    public class GCManager : MonoBehaviour
    {
        //set this to true to have the GC be manually invoked by this script when certain thresholds are reached
        public bool turn_off_mono_gc = true;

        //gc is invoked if the number of bytes allocated exceeds this value (in megabytes)
        public int manual_gc_bytes_threshold_mb = 1000;

        //however, gc will not be invoked if the number of live bytes after the most recent gc iteration multiplied by manual_gc_factor_threshold is
        //less than the current number of bytes allocated
        public float manual_gc_factor_threshold = 2;

        //if set to true, generate log messages about gc performance whenever gc is run
        public bool manual_gc_profile = true;

        //minimum sampling time for calculating expected_time_until_gc
        public float manual_gc_min_time_delta_seconds = 10;

        //set by this script every update. this is the number of bytes currently allocated
        public float allocated_mb;

        //set by this script every update. this is the average rate of memory allocation since the last gc iteration
        public float average_allocation_rate_mbps = -1;

        //set by this script every update. this is the expected number of seconds until gc runs, or -1 if unknown
        //this can be used to run gc early e.g. if the game is paused
        public float expected_time_until_gc = -1;

        //
        //

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        static Action mono_gc_disable;
        static Action mono_gc_enable;

        static bool mono_gc_loaded = false;

        static bool load_mono_gc()
        {
            if (mono_gc_loaded)
            {
                return true;
            }

            unsafe
            {
                //extracted from mono.pdb using dbh.exe (using the "enum *!*mono_gc_*" command)
                //note: for the 64 bit editor, there is only a 64 bit version of mono.pdb, so you need to also download the 32 bit editor to update this for 32 bit standalone builds
                // (you also need to decide which version of the dll to use; this can be done by comparing the mono_gc_collect offset with the two offsets for the 32 bit and 64 bit dlls)
                int offset_mono_gc_disable = 0x1b198; //0x1b310; //0x1b100
                int offset_mono_gc_enable = 0x1b1a0; //0x1b1a0; //0x1b108;
                int offset_mono_gc_collect = 0x1b14c; //0x1b2c4; //0x1b0b4; //this is used to verify that mono.dll hasn't changed

                IntPtr mono_module = GetModuleHandle("mono.dll");
                IntPtr func_ptr_mono_gc_collect = new IntPtr(mono_module.ToInt64() + offset_mono_gc_collect);
                IntPtr expected_func_ptr_mono_gc_collect = GetProcAddress(mono_module, "mono_gc_collect");
                if (func_ptr_mono_gc_collect != expected_func_ptr_mono_gc_collect)
                {
                    //if you see this error, you need to update the "offset_mono_gc_" variables above
                    Debug.Log("Cannot load gc functions. Expected collect at " + func_ptr_mono_gc_collect.ToInt64() + " Actual at " + expected_func_ptr_mono_gc_collect.ToInt64() + " Module root " + mono_module.ToInt64());
                    return false;
                }

                mono_gc_enable = (Action)Marshal.GetDelegateForFunctionPointer(new IntPtr(mono_module.ToInt64() + offset_mono_gc_enable), typeof(Action));
                mono_gc_disable = (Action)Marshal.GetDelegateForFunctionPointer(new IntPtr(mono_module.ToInt64() + offset_mono_gc_disable), typeof(Action));
            }

            mono_gc_loaded = true;
            return true;
        }

        //
        //

        //if you have a method that allocates large amounts of memory, call this at the start of it to let gc run
        public object force_enable_gc()
        {
            if (force_enable_gc_count == 0 && turn_off_mono_gc)
            {
                d_gc_disabled = false;
                mono_gc_enable();
            }

            ++force_enable_gc_count;
            var token = new force_enable_gc_token();
            token.count = force_enable_gc_count;
            return token;
        }

        //this has to be called for each call to force_enable_gc, and the object returned by force_enable_gc must be passed
        public void force_enable_gc_done(object token)
        {
            var t = (force_enable_gc_token)token;
            --force_enable_gc_count;

            t.count = -1;

            if (force_enable_gc_count == 0 && turn_off_mono_gc)
            {
                d_gc_disabled = true;
                mono_gc_disable();

                enabled = true;
            }
        }

        //alternate version of System.GC.Collect which works if gc is disabled
        public void gc_collect()
        {
            if (d_gc_disabled)
            {
                manual_gc();
            }
            else
            {
                GC.Collect();
            }
        }

        //alternate version of System.GC.GetTotalMemory
        public long gc_get_total_memory(bool do_gc)
        {
            if (do_gc)
            {
                gc_collect();
            }
            return GC.GetTotalMemory(false);
        }

        //
        //

        static bool d_gc_disabled = false;

        long manual_gc_most_recent_in_use_bytes = -1;

        System.Collections.IEnumerator run_manual_gc_after(float time)
        {
            yield return new WaitForSeconds(time);
            manual_gc();
        }

        protected void Start()
        {
            if (!load_mono_gc())
            {
                turn_off_mono_gc = false;
            }

            if (turn_off_mono_gc)
            {
                mono_gc_disable();
                d_gc_disabled = true;
            }

            StartCoroutine(run_manual_gc_after(0.1f)); //to get average_allocation_rate_mbps to work
        }

        protected void OnApplicationQuit()
        {
            StopAllCoroutines();

            if (d_gc_disabled)
            {
                manual_gc();
                mono_gc_enable();
                d_gc_disabled = false;
            }
        }

        int[] dummy_object;

        void manual_gc()
        {
            float start_time = (manual_gc_profile) ? Time.realtimeSinceStartup : 0;
            float bytes_allocated_initially = (manual_gc_profile) ? GC.GetTotalMemory(false) : 0;

            int collection_count = GC.CollectionCount(0);
            mono_gc_enable();

            //see if gc will run on its own after being enabled
            for (int x = 0; x < 100; ++x)
            {
                dummy_object = new int[1];
                dummy_object[0] = 0;
            }
            int new_collection_count = GC.CollectionCount(0);
            if (new_collection_count == collection_count)
            {
                GC.Collect(); //if not, run it manually
            }

            mono_gc_disable();

            manual_gc_most_recent_in_use_bytes = GC.GetTotalMemory(false);

            if (manual_gc_profile)
            {
                float end_time = Time.realtimeSinceStartup;
                float finalAlloc = ((float)manual_gc_most_recent_in_use_bytes) / 1024 / 1024;
                Debug.Log(
                    "Ran GC iteration.\n" +
                    "Time: " + (end_time - start_time) * 1000 + " ms\n" +
                    "Initial alloc: " + bytes_allocated_initially / 1024 / 1024 + " MB\n" +
                    "Final alloc: " + finalAlloc + " MB\n" +
                    "Util: " + (manual_gc_most_recent_in_use_bytes / bytes_allocated_initially * 100) + " %\n" +
                    "Avg. alloc. rate: " + average_allocation_rate_mbps + " MB/s\n" +
                    "Expected time until next GC: " + (manual_gc_bytes_threshold_mb - finalAlloc) / average_allocation_rate_mbps + " s\n"
                );
            }

            allocated_mb = ((float)manual_gc_most_recent_in_use_bytes) / 1024 / 1024;
            last_gc_time = Time.realtimeSinceStartup;
            last_gc_allocated_mb = allocated_mb;
        }

        float last_gc_time = -1;
        float last_gc_allocated_mb = -1;

        int force_enable_gc_count = 0;

        class force_enable_gc_token
        {
            public int count;
        };

        void monitor_gc()
        {
            if (!d_gc_disabled)
            {
                enabled = false;
                return;
            }

            long allocated_bytes = GC.GetTotalMemory(false);
            allocated_mb = ((float)allocated_bytes) / 1024 / 1024;

            float allocated_mb_limit = manual_gc_bytes_threshold_mb;
            if (manual_gc_most_recent_in_use_bytes != -1)
            {
                allocated_mb_limit = Mathf.Max(allocated_mb_limit, ((float)manual_gc_most_recent_in_use_bytes) / 1024 / 1024 * manual_gc_factor_threshold);
            }

            if (allocated_mb >= allocated_mb_limit)
            {
                manual_gc();
            }

            {
                float time = Time.realtimeSinceStartup;
                if (last_gc_time != -1)
                {
                    float delta = time - last_gc_time;
                    if (delta >= manual_gc_min_time_delta_seconds)
                    {
                        average_allocation_rate_mbps = (allocated_mb - last_gc_allocated_mb) / delta;
                    }
                }

                if (average_allocation_rate_mbps != -1)
                {
                    expected_time_until_gc = (allocated_mb_limit - allocated_mb) / average_allocation_rate_mbps;
                }
            }
        }

        protected void FixedUpdate()
        {
            monitor_gc();
        }

        protected void Update()
        {
            monitor_gc();
        }
    };
}