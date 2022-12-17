using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;

public class Serializer : MonoBehaviour // All of this is made with a reference (in class).
                                        // He entendido que serializar es una manera de "organizar" los datos y que esten en algun lado.
                                        // En el de cuando se hace, no lo se. Pero los datos que se deberían pasar (en este trabajo) son los datos varios del juego
                                        // (eso no se si incluye posicion, pero estados del personaje y parecidos si).
                                        // New update: He entendido su uso total. No se puede hacer hasta que no tengamos algo más del juego, ya que la idea sería guardar datos (ordenadamente, que esto sería serializar) sobre el juego
                                        // y luego pasarlos para utilizarlos cuando queramos (por ello la deserializacion). No creo que guardar posiciones sea una buena opcion, pero quizas podamos hacerlo como
                                        // floats por separado.
                                        // Esto se puede mantener como ejemplo de idea de como se hace.
{
    static MemoryStream stream;
    byte[] bytes;
    bool a = true;

    void Update()
    {
        if (a)
        {
            Serialize();
            Deserialize();
            a = false;
        }
    }

    void Serialize()
    {
        int intTest = 10; // TEST

        MemoryStream stream = new MemoryStream();

        /*-----WRITE VARIABLE-----*/
        BinaryWriter write = new BinaryWriter(stream);
        //write.Write(VARIABLE);
        write.Write(intTest);

        Debug.Log("Done serialize.");
        bytes = stream.ToArray();
    }

    void Deserialize()
    {
        // Read what we got in the bytes (saved data).
        stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);

        BinaryReader reader = new BinaryReader(stream);
        stream.Seek(0, SeekOrigin.Begin);

        /*-----READ-----*/ // DEBEN DE ESTAR ORDENADOS ESTOS DATOS.
        int intTestRead = reader.ReadInt32();
        Debug.Log("int test: " + intTestRead.ToString());
    }
}
