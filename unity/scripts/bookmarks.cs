/*
 * This software was created by United States Government employees
 * and may not be copyrighted.
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
*/
using UnityEngine;
using System.Collections;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System;
/*
 * Store and load bookmarks, which are xml representations of the entire simulation state.
 */
public class bookmarks : MonoBehaviour {
	static string home = null;
	static startup.instructions instructs;
	public static List<string> bookmarkList;
	static pipeBehavior pipeScript;
	static memory memoryScript;
	public static void doInit(string homePath, startup.instructions ins, pipeBehavior ps, memory ms){
		home = homePath;
		instructs = ins;
		pipeScript = ps;
		memoryScript = ms;
		bookmarkList = new List<string> ();
		string path = home + "/bookmarks/";
		System.IO.Directory.CreateDirectory (path);

		DirectoryInfo di = new DirectoryInfo(home+"/bookmarks/");
		
		FileInfo[] oldmarks = di.GetFiles();

		Array.Sort (oldmarks, (x, y) => StringComparer.OrdinalIgnoreCase.Compare (x.LastWriteTime, y.LastWriteTime));
		Debug.Log ("num files in " + home + "/bookmarks/ is " + oldmarks.Length);
		for(int i=0; i<oldmarks.Length; i++){
			bookmarkList.Add (Path.GetFileNameWithoutExtension(oldmarks[i].Name));
		}
	}


	public static void loadBookmark(string name){
		XmlDocument xmlDoc = new XmlDocument();
		string path = home + "/bookmarks/";
		xmlDoc.Load(path+name+".xml");
		//XmlNode positionNode = xmlDoc.SelectSingleNode("//bookmark/position");
		//instructs.setPosition(long.Parse (positionNode.InnerText));
		functionDatabase.fromXML(xmlDoc);
		manageControl.fromXML(xmlDoc);
		pipeScript.fromXML (xmlDoc);
		memoryScript.fromXml (xmlDoc);
	}
	public static void createBookmark(string name){
		XmlDocument xmlDoc = new XmlDocument();
		XmlNode rootNode = xmlDoc.CreateElement("bookmark");
		xmlDoc.AppendChild(rootNode);
		XmlNode positionNode = xmlDoc.CreateElement ("position");
		positionNode.InnerText = instructs.getPosition ().ToString ();
		rootNode.AppendChild (positionNode);
		XmlNode functionsNode = xmlDoc.CreateElement("functions");
		functionDatabase.toXML(ref xmlDoc, ref functionsNode);
		rootNode.AppendChild(functionsNode);
		manageControl.toXML(ref xmlDoc, ref rootNode);
		memoryScript.toXML (ref xmlDoc, ref rootNode);
		pipeScript.toXML (ref xmlDoc, ref rootNode);
		string path = home + "/bookmarks/";

		xmlDoc.Save(path+name+".xml");
		bookmarkList.Add (name);
		//Debug.Log ("saved bookmark to " + path + name + ".xml");
	}
}
