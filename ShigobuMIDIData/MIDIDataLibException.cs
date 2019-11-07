using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	class MIDIDataLibException : System.Exception
	{
		public MIDIDataLibException() : base("MIDIDataLibで不明な例外が発生しました。"){ }

		public MIDIDataLibException(string message) : base(message) { }

		public MIDIDataLibException(string message, Exception innerException) : base(message, innerException) { }

	}
}
