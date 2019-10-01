using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
    public class Data
    {
		int Format { get; set; }            /* SMFフォーマット(0/1) */
		int NumTrack { get; set; }          /* トラック数(0～∞) */
		int TimeBase { get; set; }          /* タイムベース(例：120) */
		Track FirstTrack { get; set; } /* 最初のトラックへのポインタ(なければNULL) */
		Track LastTrack { get; set; }  /* 最後のトラックへのポインタ(なければNULL) */
		Track NextSeq { get; set; }    /* 次のシーケンスへのポインタ(なければNULL) */
		Track PrevSeq { get; set; }    /* 前のシーケンスへのポインタ(なければNULL) */
	}
}
