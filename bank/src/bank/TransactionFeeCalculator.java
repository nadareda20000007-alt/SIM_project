
package bank;

public interface TransactionFeeCalculator {
    double calculateFee(double amount);
    boolean isFeeApplicable(double amount);
}

class StandardFeeCalculator implements TransactionFeeCalculator {
    private double flatFee;
    private double percentageFee;

    public StandardFeeCalculator(double flatFee, double percentageFee) {
        this.flatFee = flatFee;
        this.percentageFee = percentageFee;
    }

    @Override
    public double calculateFee(double amount) {
        return flatFee + (amount * percentageFee / 100);
    }

    @Override
    public boolean isFeeApplicable(double amount) {
        return amount > 1000;
    }
}