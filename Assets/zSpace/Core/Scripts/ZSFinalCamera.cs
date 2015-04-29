//////////////////////////////////////////////////////////////////////////
//
//  Copyright (C) 2007-2014 zSpace, Inc.  All Rights Reserved.
//
//  File:       ZSFinalCamera.cs
//  Content:    The zSpace final camera script.
//
//////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class ZSFinalCamera : MonoBehaviour
{
    void OnPreCull()
    {
        // Signal that the frame is done and reset the Unity's OpenGl state.
        GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.FrameDone);
        GL.IssuePluginEvent((int)ZSCore.GlPluginEventType.DisableStereo);
        GL.InvalidateState();

        gameObject.camera.enabled = false;
    }
}
