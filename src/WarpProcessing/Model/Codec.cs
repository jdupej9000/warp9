using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public interface IEncoderConfig
    {
    }

    public interface IDecoderConfig
    {
    }

    public abstract class Codec
    {
        public abstract bool TryEncodeObject(Stream stream, object value, IEncoderConfig? cfg);
        public abstract bool TryEncode<T>(Stream stream, T value, IEncoderConfig? cfg);
        public abstract bool TryDecode<T>(Stream stream, IDecoderConfig? cfg, [MaybeNullWhen(false)] out T? value);
    }

    public class Codec<T> : Codec
    {
        public Codec(Action<Stream, T, IEncoderConfig?>? enc, Func<Stream, IDecoderConfig?, T?>? dec)
        {
            encoder = enc;
            decoder = dec;
        }

        private Action<Stream, T, IEncoderConfig?>? encoder;
        private Func<Stream, IDecoderConfig?, T?>? decoder;

        public override bool TryEncodeObject(Stream stream, object value, IEncoderConfig? cfg)
        {
            if (encoder is not null && value is T typedValue)
            {
                encoder(stream, typedValue, cfg);
                return true;
            }

            return false;
        }

        public override bool TryEncode<TVal>(Stream stream, TVal value, IEncoderConfig? cfg)
        {
            if (encoder is Action<Stream, TVal, IEncoderConfig?> enc)
            {
                enc(stream, value, cfg);
                return true;
            }

            return false;
        }

        public override bool TryDecode<TVal>(Stream stream, IDecoderConfig? cfg, [MaybeNullWhen(false)] out TVal value)
        {
            if (decoder is Func<Stream, IDecoderConfig?, TVal?> dec)
            {
                value = dec(stream, cfg);
                return value is not null;
            }

            value = default;
            return false;
        }
    }
}
