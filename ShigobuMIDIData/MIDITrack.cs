using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	class Track
	{
		int TempIndex { get; set; }                  /* このトラックの一時的なインデックス(0から始まる) */
		int NumEvent { get; set; }                   /* トラック内のイベント数 */
		Event FirstEvent { get; set; } /* 最初のイベントへのポインタ(なければNULL) */
		Event LastEvent { get; set; }  /* 最後のイベントへのポインタ(なければNULL) */
		Track PrevTrack { get; set; }  /* 前のトラックへのポインタ(なければNULL) */
		Track NextTrack { get; set; }  /* 次のトラックへのポインタ(なければNULL) */
		Data Parent { get; set; }                   /* 親(MIDIDataオブジェクト)へのポインタ */
	}
}
