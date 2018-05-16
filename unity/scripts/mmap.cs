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
using System.Collections.Generic;
/*
 * Handle creation and removal of memory regions resulting from mmap and munmap operations.
 * TBD generalize management of where to put new memory regions.
 */
public class mmap : MonoBehaviour {
	private static List<region> uninstantiated;
	private static List<memory> memoryRegions;

	private class region{
		public uint address;
		public int count;
		public region(uint address, int count){
			this.address = address;
			this.count = count;
		}
	}
	public static void newRegion(uint address, int count){
		region r = new region (address, count);
		if(pipeBehavior.sessionNumber == 0){
			uninstantiated.Add (r);
		}else{
			createRegion(address, count);
		}

	}
	public static void setMemoryColor(Color color){
		memory.memoryColor = color;
		for(int i=0; i<memoryRegions.Count; i++){
			memoryRegions[i].updateColor();
		}
	}
	public static void setCubeColor(Color oldColor, Color newColor){
		for(int i=0; i<memoryRegions.Count; i++){
			memoryRegions[i].setCubeColor(oldColor, newColor);
		}
	}
	private static bool openSpace(Vector3 pt){
		bool retval = true;
		for(int i=0; i<memoryRegions.Count; i++){
			if(memoryRegions[i].transform.position == pt){
				return false;
			}
		}
		return retval;
	}
	private static Vector3 getOpenSpace(){
		Vector3 memPos = startup.memoryStart;
		memPos.y -= 8;
		//memPos.x += 10;
		memPos.x -= 1.4f;
		//memPos.x -= 5;
		memPos.z -= 20;
		for(int j=0; j<2; j++){
			for(int i=0; i<4; i++){
				memPos.y = memPos.y + i*5;
				memPos.x = memPos.x + j*5;
				if(openSpace(memPos)){
					return memPos;
				}
			}
		}
		return Vector3.zero;
	}

	private static void createRegion(uint address, int count){
		GameObject newMemory = Instantiate (Resources.Load ("memoryPrefab")) as GameObject;
		newMemory.name = "mem"+address.ToString("x");
		Vector3 memPos = getOpenSpace();
		newMemory.transform.position = memPos;
		memory script = (memory) newMemory.GetComponent(typeof(memory));
		uint max_address = (uint) (address+count-1);
		script.setRange(address, max_address);
		memoryRegions.Add (script);
	}
	public static memory findMemoryRegion(uint address){
		for(int i=0; i<memoryRegions.Count; i++){
 			//Debug.Log ("findMemoryRegions "+i+" for "+address.ToString("x")+" min "+memoryRegions[i].minDataAddress.ToString("x")+" max "+memoryRegions[i].maxDataAddress.ToString("x"));
			if(memoryRegions[i].inRange(address)){
				return memoryRegions[i];
			}
		}
 		Debug.Log ("findMemoryRegion failed to find region for address " + address.ToString ("x"));
		//Debug.Break ();
		return null;
	}
	public static void firstSession(){
		for(int i=0; i<uninstantiated.Count; i++){
			Debug.Log ("mmap region from "+uninstantiated[i].address.ToString("x")+" to "+uninstantiated[i].count);
			createRegion(uninstantiated[i].address, uninstantiated[i].count);
		}
	}
	public static void removeRegion(uint address, int count){
		if(pipeBehavior.sessionNumber == 0){
			for(int i=0; i<uninstantiated.Count; i++){
				if(uninstantiated[i].address == address){
					uninstantiated.RemoveAt(i);
					break;
				}
			}
		}else{
			memory region = findMemoryRegion(address);
			//TBD check count, handle partial giveback?
			if(region == null){
				Debug.Log ("manageControl found munmap of unmapped memory "+address.ToString("x"));
				Debug.Break ();
				return;
			}
			region.remove();
			memoryRegions.Remove(region);
			Destroy(region.gameObject);
		}
	}
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	static public void doInit(){
		uninstantiated = new List<region> ();
		GameObject memoryObject = GameObject.Find("Memory1");
		memory memoryScript = (memory) memoryObject.GetComponent(typeof(memory));
		memoryRegions = new List<memory> ();
		memoryRegions.Add (memoryScript);
	}
		
}
