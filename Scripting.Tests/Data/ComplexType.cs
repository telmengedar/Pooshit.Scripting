namespace Scripting.Tests.Data {
    public class ComplexType {

        public ComplexType() {
        }

        public ComplexType(Parameter parameter, int count) {
            Parameter = parameter;
            Count = count;
        }

        public ComplexType(Parameter parameter, int count, int[] numbers) {
            Parameter = parameter;
            Count = count;
            Numbers = numbers;
        }

        public Parameter Parameter { get; set; }
        public int Count { get; set; }

        public int[] Numbers { get; set; }
    }
}