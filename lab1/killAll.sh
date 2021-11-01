for number in $(pgrep dotnetLab1)
do
    echo "Killing $number"
    kill $number
done