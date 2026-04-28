
package bank;

public class Transaction {
    private String type;
    private double amount;
    private double newBalance;
    private String date;

    public Transaction(String type, double amount, double newBalance) {
        this.type = type;
        this.amount = amount;
        this.newBalance = newBalance;
        this.date = java.time.LocalDate.now().toString();
    }

    public void displayTransaction() {
        System.out.printf("%s | $%.2f | New Balance: $%.2f | Date: %s%n", 
                         type, amount, newBalance, date);
    }
}