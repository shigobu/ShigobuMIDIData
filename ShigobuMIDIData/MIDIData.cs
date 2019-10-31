using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	public enum Formats
	{
		Format0,
		Format1,
		Format2
	}

	public class Data
    {
		public Formats Format { get; set; }            /* SMFフォーマット(0/1) */
		public int NumTrack { get; set; }          /* トラック数(0～∞) */
		public int TimeBase { get; set; }          /* タイムベース(例：120) */
		public Track FirstTrack { get; set; } /* 最初のトラックへのポインタ(なければNULL) */
		public Track LastTrack { get; set; }  /* 最後のトラックへのポインタ(なければNULL) */
		public Track NextSeq { get; set; }    /* 次のシーケンスへのポインタ(なければNULL) */
		public Track PrevSeq { get; set; }    /* 前のシーケンスへのポインタ(なければNULL) */
	}
}
