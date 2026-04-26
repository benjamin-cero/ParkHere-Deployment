void main() {
  // Test with the EXACT format from our API
  var s = "2026-04-26T01:15:00.0000000";
  var parsed = DateTime.parse(s);
  print("Input: $s");
  print("Parsed: $parsed  isUtc: ${parsed.isUtc}");
  print("toLocal(): ${parsed.toLocal()}  isUtc: ${parsed.toLocal().isUtc}");
  print("hour: ${parsed.hour} vs toLocal hour: ${parsed.toLocal().hour}");
  print("");
  
  // Also test the "now" format
  var now = DateTime.now();
  print("DateTime.now(): $now  isUtc: ${now.isUtc}");
  print("Difference test: ${parsed.difference(now)}");
  print("toLocal diff: ${parsed.toLocal().difference(now)}");
}
