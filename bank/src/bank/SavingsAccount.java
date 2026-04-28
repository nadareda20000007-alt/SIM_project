
package bank;
public class SavingsAccount extends Account {
    private double interestRate;

    public SavingsAccount(String accountNumber, String accountHolderName, double initialBalance, double interestRate) {
        super(accountNumber, accountHolderName, initialBalance);
        this.interestRate = interestRate;
    }

    @Override
    public void deposit(double amount) {
        if (amount > 0) {
            balance += amount;
            addTransaction("DEPOSIT", amount, balance);
            System.out.println("Deposited $" + amount + " to Savings Account");
        }
    }

    @Override
    public boolean withdraw(double amount) {
        if (amount > 0 && amount <= balance && amount <= 1000) { // Daily limit
            balance -= amount;
            addTransaction("WITHDRAWAL", amount, balance);
            System.out.println("Withdrew $" + amount + " from Savings Account");
            return true;
        }
        else{
        System.out.println("Insufficient balance or withdrawal limit exceeded!");
        return false;
        }
    }

    @Override
    public double calculateInterest() {
        return balance * (interestRate / 100 / 12); // Monthly interest
    }

    public void applyMonthlyInterest() {
        double interest = calculateInterest();
        deposit(interest, "Monthly Interest");
    }

    @Override
    public String toString() {
        return "Savings Account - Balance: $" + String.format("%.2f", balance) + 
               ", Interest Rate: " + interestRate + "%";
    }
}