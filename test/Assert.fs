module Assert

open NUnit.Framework


let AreEqual (expected, value) =
  // If a type parameter in the expected value's type is unconstrained, the equality
  // check may fail without the annotation, because the assert will compare
  // the two values as objects with potentially different types. We don't expect to
  // ever want to assert equality on 2 things determined to have different types
  // (and if we do, we can always cast the arguments to objs).
  Assert.AreEqual(
    (expected : 'a),
    (value : 'a),
    // We include a printout of the types as F# values since [Assert.AreEqual] normally
    // uses [Object.ToString()] which is not helpful for common types like [Result]s.
    sprintf "AreEqual(%A, %A)" expected value
  )

let MatchesIgnoringLines (pattern, value) =
  Assert.That(
    (sprintf "%A" value)
      .Replace("\n", "")
      .Replace("\r", ""),
    Does.Match(pattern : string)
  )

let Unreached (_ : _) = failwith "Not expected to reach this code"
