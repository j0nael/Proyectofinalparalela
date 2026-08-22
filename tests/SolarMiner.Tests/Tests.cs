using SolarMiner;

// ============================================================================
//  PRUEBAS DEL NODO MINERO
//  Tests ligeros sin framework externo (para no complicar la compilación).
//  Se ejecutan con:  dotnet run --project tests/SolarMiner.Tests
//  Cada prueba imprime PASA / FALLA. Cubre la carpeta /tests del repositorio.
// ============================================================================

int passed = 0, failed = 0;

void Check(string name, bool condition)
{
    if (condition) { Console.WriteLine($"  [PASA] {name}"); passed++; }
    else { Console.WriteLine($"  [FALLA] {name}"); failed++; }
}

Console.WriteLine("Ejecutando pruebas del nodo minero...\n");

// 1. El hash es determinista: misma entrada -> mismo hash.
string h1 = Block.ComputeHash("bloque de prueba");
string h2 = Block.ComputeHash("bloque de prueba");
Check("Hash determinista (misma entrada da mismo hash)", h1 == h2);

// 2. Cambio mínimo en la entrada cambia el hash por completo.
string h3 = Block.ComputeHash("bloque de pruebA");
Check("Efecto avalancha (cambio mínimo cambia el hash)", h1 != h3);

// 3. El hash SHA-256 mide 64 caracteres hexadecimales.
Check("Longitud del hash SHA-256 = 64 hex", h1.Length == 64);

// 4. MeetsDifficulty acepta un hash con los ceros requeridos.
Check("Dificultad OK cuando hay ceros suficientes",
      Miner.MeetsDifficulty("0000abcdef", 4));

// 5. MeetsDifficulty rechaza un hash con menos ceros.
Check("Dificultad rechaza cuando faltan ceros",
      !Miner.MeetsDifficulty("00abcdef", 4));

// 6. El minado secuencial encuentra un nonce válido (dificultad baja para rapidez).
var miner = new Miner();
var txs = CooperativeData.GenerateTransactions(5);
var block = new Block(1, txs, new string('0', 64));
var result = miner.MineSequential(block, difficulty: 3);
Check("Minado secuencial produce hash que cumple la dificultad",
      Miner.MeetsDifficulty(result.Hash, 3));

// 7. El nonce ganador es VERIFICABLE: recalcular el hash da el mismo resultado válido.
string recomputed = Block.ComputeHash(block.GetBlockContent(result.Nonce));
Check("El nonce ganador es verificable (validación instantánea)",
      recomputed == result.Hash && Miner.MeetsDifficulty(recomputed, 3));

// 8. El minado paralelo también encuentra un hash válido.
var block2 = new Block(1, txs, new string('0', 64));
var resultPar = miner.MineParallel(block2, difficulty: 3, threadCount: 4);
Check("Minado paralelo produce hash que cumple la dificultad",
      Miner.MeetsDifficulty(resultPar.Hash, 3));

// 9. Determinismo: dos bloques con el MISMO timestamp fijo plantean el mismo
//    acertijo y por tanto encuentran el mismo nonce ganador. Esto es lo que hace
//    justa la comparación secuencial vs paralela.
var fixedTime = new DateTime(2026, 8, 3, 8, 0, 0, DateTimeKind.Utc);
var blockA = new Block(7, txs, new string('0', 64), fixedTime);
var blockB = new Block(7, txs, new string('0', 64), fixedTime);
var rA = miner.MineSequential(blockA, difficulty: 3);
var rB = miner.MineSequential(blockB, difficulty: 3);
Check("Bloques con timestamp fijo dan el MISMO nonce (comparación justa)",
      rA.Nonce == rB.Nonce && rA.Hash == rB.Hash);

// 10. Secuencial y paralelo, sobre el MISMO acertijo, hallan el mismo nonce.
var blockC = new Block(7, txs, new string('0', 64), fixedTime);
var blockD = new Block(7, txs, new string('0', 64), fixedTime);
var rSeq = miner.MineSequential(blockC, difficulty: 3);
var rPar = miner.MineParallel(blockD, difficulty: 3, threadCount: 4);
Check("Secuencial y paralelo coinciden en el nonce ganador",
      rSeq.Nonce == rPar.Nonce);

// 11. El prefijo + nonce reconstruye exactamente el contenido completo (optimización).
var blockE = new Block(1, txs, new string('0', 64), fixedTime);
Check("Prefijo + nonce == contenido completo del bloque",
      blockE.GetContentPrefix() + "12345" == blockE.GetBlockContent(12345));

Console.WriteLine($"\nResultado: {passed} pasaron, {failed} fallaron.");
Environment.Exit(failed == 0 ? 0 : 1);
