using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	public class Track
	{
		public int TempIndex { get; set; }                  /* このトラックの一時的なインデックス(0から始まる) */
		public int NumEvent { get; set; }                   /* トラック内のイベント数 */
		public Event FirstEvent { get; set; } /* 最初のイベントへのポインタ(なければNULL) */
		public Event LastEvent { get; set; }  /* 最後のイベントへのポインタ(なければNULL) */
		public Track PrevTrack { get; set; }  /* 前のトラックへのポインタ(なければNULL) */
		public Track NextTrack { get; set; }  /* 次のトラックへのポインタ(なければNULL) */
		public Data Parent { get; set; }                   /* 親(MIDIDataオブジェクト)へのポインタ */
	}
}
