// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------
using System;
using UnityEngine;

public class Logger
{
	public static bool isDebug = true;

	public Logger()
	{
	}

	public static void INFO(string text)
	{
		if (isDebug)
			Debug.Log("<INFO> " + text);
	}

	public static void TRACE(string text)
	{
		if (isDebug)
			Debug.Log("<TRACE> " + text);
	}

	public static void WARN(string text)
	{
		if (isDebug)
			Debug.Log("<WARN> " + text);
	}

	public static void LOG(string text)
	{
		if (isDebug)
			Debug.Log("<LOG> " + text);
	}
}
