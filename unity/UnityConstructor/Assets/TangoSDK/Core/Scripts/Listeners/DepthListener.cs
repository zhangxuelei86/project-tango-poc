﻿//-----------------------------------------------------------------------
// <copyright file="DepthListener.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Tango
{
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// Delegate for Tango depth events.
    /// </summary>
    /// <param name="tangoDepth">TangoUnityDepth object for the available depth frame.</param>
    internal delegate void OnTangoDepthAvailableEventHandler(TangoUnityDepth tangoDepth);
    
    /// <summary>
    /// Delegate for Tango depth event that can be called on any thread.
    /// </summary>
    /// <param name="tangoDepth">TangoXYZij object for the available depth frame.</param>
    internal delegate void OnTangoDepthMulithreadedAvailableEventHandler(TangoXYZij tangoDepth);

    /// <summary>
    /// Marshals Tango depth data between the C callbacks in one thread and
    /// the main Unity thread.
    /// </summary>
    internal class DepthListener
    {
        private Tango.DepthProvider.TangoService_onDepthAvailable m_onDepthAvailableCallback;

        private bool m_isDirty = false;
        private TangoUnityDepth m_tangoDepth;
        private System.Object m_lockObject = new System.Object();

        /// <summary>
        /// Called when a new Tango depth is available.
        /// </summary>
        private OnTangoDepthAvailableEventHandler m_onTangoDepthAvailable;

        /// <summary>
        /// Called when a new Tango depth is available on the thread the depth came from.
        /// </summary>
        private OnTangoDepthMulithreadedAvailableEventHandler m_onTangoDepthMultithreadedAvailable;

        /// <summary>
        /// Register to get Tango depth callbacks.
        /// 
        /// NOTE: Tango depth callbacks happen on a different thread than the main
        /// Unity thread.
        /// </summary>
        internal virtual void SetCallback()
        {
            m_tangoDepth = new TangoUnityDepth();
            m_onDepthAvailableCallback = new Tango.DepthProvider.TangoService_onDepthAvailable(_OnDepthAvailable);
            Tango.DepthProvider.SetCallback(m_onDepthAvailableCallback);
        }

        /// <summary>
        /// Raise a Tango depth event if there is new data.
        /// </summary>
        internal void SendDepthIfAvailable()
        {
            if (m_isDirty && m_onTangoDepthAvailable != null)
            {
                lock (m_lockObject)
                {
                    m_onTangoDepthAvailable(m_tangoDepth);
                }

                m_isDirty = false;
            }
        }

        /// <summary>
        /// Register a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthAvailable += handler;
            }
        }

        /// <summary>
        /// Unregisters a Unity main thread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnTangoDepthAvailable(OnTangoDepthAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthAvailable -= handler;
            }
        }

        /// <summary>
        /// Register a multithread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to register.</param>
        internal void RegisterOnTangoDepthMultithreadedAvailable(OnTangoDepthMulithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthMultithreadedAvailable += handler;
            }
        }
        
        /// <summary>
        /// Unregisters a multithread handler for the Tango depth event.
        /// </summary>
        /// <param name="handler">Event handler to unregister.</param>
        internal void UnregisterOnTangoDepthMultithreadedAvailable(OnTangoDepthMulithreadedAvailableEventHandler handler)
        {
            if (handler != null)
            {
                m_onTangoDepthMultithreadedAvailable -= handler;
            }
        }

        /// <summary>
        /// Callback that gets called when depth is available from the Tango Service.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="xyzij">The depth data returned from Tango.</param>
        private void _OnDepthAvailable(IntPtr callbackContext, TangoXYZij xyzij)
        {
            // Fill in the data to draw the point cloud.
            if (xyzij != null)
            {
                if (m_onTangoDepthMultithreadedAvailable != null)
                {
                    m_onTangoDepthMultithreadedAvailable(xyzij);
                }

                lock (m_lockObject)
                {
                    // copy single members
                    m_tangoDepth.m_version = xyzij.version;
                    m_tangoDepth.m_timestamp = xyzij.timestamp;
                    m_tangoDepth.m_ijColumns = xyzij.ij_cols;
                    m_tangoDepth.m_ijRows = xyzij.ij_rows;
                    m_tangoDepth.m_pointCount = xyzij.xyz_count;

                    // deep copy arrays
                    
                    // Fill in the data to draw the point cloud.
                    if (xyzij != null)
                    {
                        int numberOfActivePoints = xyzij.xyz_count * 3;

                        // copy new points
                        if (numberOfActivePoints > 0)
                        {
                            Marshal.Copy(xyzij.xyz, m_tangoDepth.m_points, 0, numberOfActivePoints);
                            m_isDirty = true;
                        }
                    }
                }
            }
        }
    }
}