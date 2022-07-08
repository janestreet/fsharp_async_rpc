module Assert

val AreEqual : expected : 'a * value : 'a -> unit
val MatchesIgnoringLines : pattern : string * value : 'a -> unit

val Unreached : _ -> _
